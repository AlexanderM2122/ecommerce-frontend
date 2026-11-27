using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using EcommerceFrontend.Models;
using EcommerceFrontend.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EcommerceFrontend.Controllers;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[EnableRateLimiting("auth")]
[Authorize]
public class AuthController(AuthClientService auth, IAccessTokenStore tokenStore, IDataProtectionProvider protection) : Controller
{
    private const string Scheme = CookieAuthenticationDefaults.AuthenticationScheme;
    private const string GenericEmailMessage = "Si la cuenta cumple los requisitos, recibirás un correo con el código.";
    private const string PendingCookie = "__Host-EcommercePendingEmail";

    [HttpGet, AllowAnonymous]
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");
        return PendingEmail() is not null ? RedirectToAction(nameof(VerificarCorreo)) : View(new Login());
    }

    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> Index(Login model, CancellationToken ct)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");
        if (!ModelState.IsValid)
            return View(model);

        var result = await Post<AuthUser>("", model, ct);
        if (result.Status == HttpStatusCode.Forbidden && result.Code == "correo_pendiente" && result.Email is { Length: > 0 } email)
        {
            SetPendingEmail(email);
            return RedirectToAction(nameof(VerificarCorreo));
        }
        if (!result.Success)
        {
            AddError(result);
            return View(model);
        }

        if (result.Status == HttpStatusCode.Accepted && ValidToken(result.Data?.Reto))
            return View(nameof(SegundoFactor), new SegundoFactorForm { Reto = result.Data!.Reto! });

        return await StartSession(result.Data, nameof(Index), model);
    }

    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> SegundoFactor(SegundoFactorForm model, CancellationToken ct)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");
        if (!ValidToken(model.Reto))
            return RedirectToAction(nameof(Index));
        if (!ModelState.IsValid)
            return View(model);

        var result = await Post<AuthUser>("/mfa/login", model, ct);
        if (result.Status == HttpStatusCode.Unauthorized)
        {
            TempData["AuthError"] = result.Message ?? "Vuelve a ingresar para iniciar sesión.";
            return RedirectToAction(nameof(Index));
        }
        if (!result.Success)
        {
            AddError(result);
            return View(model);
        }
        return await StartSession(result.Data, nameof(SegundoFactor), model);
    }

    private async Task<IActionResult> StartSession(AuthUser? user, string view, object model)
    {
        if (string.IsNullOrWhiteSpace(user?.Email) || string.IsNullOrWhiteSpace(user.Nombre) ||
            string.IsNullOrWhiteSpace(user.Jwt) || user.Rol is not ("Administrador" or "Usuario"))
        {
            ModelState.AddModelError(string.Empty, "La respuesta del servidor no es válida.");
            return View(view, model);
        }

        var sessionId = tokenStore.Create(user.Jwt);
        var identity = new ClaimsIdentity(
            [
                new(ClaimTypes.Name, user.Email),
                new(ClaimTypes.GivenName, user.Nombre),
                new(ClaimTypes.Role, user.Rol),
                new("session_id", sessionId)
            ], Scheme);
        try
        {
            await HttpContext.SignInAsync(Scheme, new ClaimsPrincipal(identity), new AuthenticationProperties
            {
                IsPersistent = false,
                AllowRefresh = false,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(19)
            });
        }
        catch
        {
            tokenStore.Remove(sessionId);
            throw;
        }
        ClearPendingEmail();
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> Salir()
    {
        var sessionId = User.FindFirstValue("session_id");
        if (sessionId is not null) tokenStore.Remove(sessionId);
        TempData.Remove("MfaSetup");
        await HttpContext.SignOutAsync(Scheme);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet, AllowAnonymous]
    public IActionResult Registro()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");
        return View(new RegistroForm());
    }

    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> Registro(RegistroForm model, CancellationToken ct)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");
        if (!ModelState.IsValid)
            return View(model);

        var result = await Post<AuthUser>("/registro", new { model.Nombre, model.Email, model.Password }, ct);
        if (!result.Success)
        {
            AddError(result);
            return View(model);
        }

        SetPendingEmail(result.Data?.Email is { Length: > 0 } email ? email : model.Email.Trim());
        TempData["CodeMessage"] = "Cuenta creada. Revisa tu correo para obtener el código.";
        return RedirectToAction(nameof(VerificarCorreo));
    }

    private ITimeLimitedDataProtector PendingProtector =>
        protection.CreateProtector("EcommerceFrontend.Auth.PendingEmail").ToTimeLimitedDataProtector();

    private void SetPendingEmail(string email) =>
        Response.Cookies.Append(PendingCookie, PendingProtector.Protect(email, TimeSpan.FromDays(1)),
            new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax, Path = "/", MaxAge = TimeSpan.FromDays(1) });

    private string? PendingEmail()
    {
        if (!Request.Cookies.TryGetValue(PendingCookie, out var encrypted))
            return null;
        try { return PendingProtector.Unprotect(encrypted); }
        catch (CryptographicException) { return null; }
    }

    private void ClearPendingEmail() =>
        Response.Cookies.Delete(PendingCookie, new CookieOptions { Secure = true, HttpOnly = true, SameSite = SameSiteMode.Lax, Path = "/" });

    [HttpPost, AllowAnonymous]
    public IActionResult CambiarCuenta()
    {
        ClearPendingEmail();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet, AllowAnonymous]
    public IActionResult ReenviarVerificacion() =>
        PendingEmail() is not null ? RedirectToAction(nameof(VerificarCorreo)) : EmailView(new(), true);

    [HttpPost, AllowAnonymous]
    public Task<IActionResult> ReenviarVerificacion(CorreoCuentaForm model, CancellationToken ct) =>
        RequestEmail(model, true, ct);

    [HttpGet, AllowAnonymous]
    public IActionResult RecuperarPassword() => EmailView(new(), false);

    [HttpPost, AllowAnonymous]
    public Task<IActionResult> RecuperarPassword(CorreoCuentaForm model, CancellationToken ct) =>
        RequestEmail(model, false, ct);

    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> ReenviarCodigo(bool verificar, CancellationToken ct)
    {
        var email = verificar ? PendingEmail() : RecoveryEmail();
        if (email is null)
            return RedirectToAction(verificar ? nameof(ReenviarVerificacion) : nameof(RecuperarPassword));

        var path = verificar ? "/reenviar-verificacion" : "/recuperar-password";
        var result = await Post<object>(path, new { email }, ct);
        if (!result.Success)
        {
            AddError(result);
            return CodeView(email, verificar, new());
        }

        if (!verificar)
            TempData.Remove("PasswordGrant");
        TempData["CodeMessage"] = GenericEmailMessage;
        return RedirectToAction(verificar ? nameof(VerificarCorreo) : nameof(VerificarRecuperacion));
    }

    private IActionResult EmailView(CorreoCuentaForm model, bool verify)
    {
        ViewData["Verificar"] = verify;
        return View("CorreoCuenta", model);
    }

    private async Task<IActionResult> RequestEmail(CorreoCuentaForm model, bool verify, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return EmailView(model, verify);
        var path = verify ? "/reenviar-verificacion" : "/recuperar-password";
        var result = await Post<object>(path, model, ct);
        if (!result.Success)
        {
            AddError(result);
            return EmailView(model, verify);
        }

        var email = model.Email.Trim();
        if (verify)
        {
            SetPendingEmail(email);
        }
        else
        {
            TempData["RecoveryEmail"] = email;
            TempData.Remove("PasswordGrant");
        }
        TempData["CodeMessage"] = GenericEmailMessage;
        return RedirectToAction(verify ? nameof(VerificarCorreo) : nameof(VerificarRecuperacion));
    }

    [HttpGet, AllowAnonymous]
    public IActionResult VerificarCorreo() =>
        PendingEmail() is { } email ? CodeView(email, true, new()) : RedirectToAction(nameof(ReenviarVerificacion));

    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> VerificarCorreo(CodigoCorreoForm model, CancellationToken ct)
    {
        if (PendingEmail() is not { } email)
            return RedirectToAction(nameof(ReenviarVerificacion));
        if (!ModelState.IsValid)
            return CodeView(email, true, model);

        var result = await Post<object>("/verificar-correo", new { email, model.Codigo }, ct);
        if (!result.Success)
        {
            AddError(result);
            return CodeView(email, true, model);
        }

        ClearPendingEmail();
        TempData["AuthMessage"] = "Correo verificado. Ya puedes iniciar sesión.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet, AllowAnonymous]
    public IActionResult VerificarRecuperacion() =>
        RecoveryEmail() is { } email ? CodeView(email, false, new()) : RedirectToAction(nameof(RecuperarPassword));

    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> VerificarRecuperacion(CodigoCorreoForm model, CancellationToken ct)
    {
        if (RecoveryEmail() is not { } email)
            return RedirectToAction(nameof(RecuperarPassword));
        if (!ModelState.IsValid)
            return CodeView(email, false, model);

        var result = await Post<PasswordGrant>("/verificar-recuperacion", new { email, model.Codigo }, ct);
        if (!result.Success || !ValidToken(result.Data?.Token))
        {
            AddError(result);
            return CodeView(email, false, model);
        }

        TempData["PasswordGrant"] = result.Data!.Token;
        return RedirectToAction(nameof(RestablecerPassword));
    }

    private string? RecoveryEmail() => TempData.Peek("RecoveryEmail") as string;

    private IActionResult CodeView(string email, bool verify, CodigoCorreoForm model)
    {
        ViewData["Verificar"] = verify;
        ViewData["Email"] = email;
        return View("CodigoCorreo", model);
    }

    [HttpGet, AllowAnonymous]
    public IActionResult RestablecerPassword() =>
        ValidToken(TempData.Peek("PasswordGrant") as string)
            ? View(new RestablecerCuentaForm())
            : RedirectToAction(nameof(VerificarRecuperacion));

    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> RestablecerPassword(RestablecerCuentaForm model, CancellationToken ct)
    {
        var token = TempData.Peek("PasswordGrant") as string;
        if (!ValidToken(token))
            return RedirectToAction(nameof(VerificarRecuperacion));
        if (!ModelState.IsValid)
            return View(model);

        var result = await Post<object>("/restablecer-password", new { token, model.Password }, ct);
        if (result.Status == HttpStatusCode.Gone)
        {
            TempData.Remove("PasswordGrant");
            TempData["CodeError"] = result.Message ?? "La autorización venció. Solicita otro código.";
            return RedirectToAction(nameof(VerificarRecuperacion));
        }
        if (!result.Success)
        {
            AddError(result);
            return View(model);
        }

        TempData.Remove("PasswordGrant");
        TempData.Remove("RecoveryEmail");
        var sessionId = User.FindFirstValue("session_id");
        if (sessionId is not null) tokenStore.Remove(sessionId);
        TempData.Remove("MfaSetup");
        await HttpContext.SignOutAsync(Scheme);
        TempData["AuthMessage"] = "Contraseña actualizada. Inicia sesión con tu contraseña nueva.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Perfil() => View(new CambiarPasswordForm());

    [HttpPost]
    public async Task<IActionResult> CambiarPassword(CambiarPasswordForm model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(nameof(Perfil), model);

        var result = await Post<object>("/cambiar-password", new { model.PasswordActual, model.Password }, ct, CurrentJwt());
        if (result.Status == HttpStatusCode.Unauthorized)
            return await Salir();
        if (!result.Success)
        {
            AddError(result);
            return View(nameof(Perfil), model);
        }

        TempData.Remove("PasswordGrant");
        TempData.Remove("RecoveryEmail");
        TempData["AuthMessage"] = "Contraseña actualizada. Inicia sesión con tu contraseña nueva.";
        return await Salir();
    }

    [HttpGet]
    public async Task<IActionResult> Seguridad(CancellationToken ct)
    {
        var result = await auth.SendAsync<MfaState>(HttpMethod.Get, "/mfa", null, ct, CurrentJwt());
        if (!result.Success)
        {
            if (result.Status == HttpStatusCode.Unauthorized) return await Salir();
            AddError(result);
            return View(new SeguridadCuentaView(false));
        }
        return View(new SeguridadCuentaView(result.Data?.Activo == true));
    }

    [HttpPost]
    public async Task<IActionResult> PrepararMfa(string? password, CancellationToken ct)
    {
        var result = await Post<MfaSetup>("/mfa/preparar", new { password }, ct, CurrentJwt());
        if (result.Status == HttpStatusCode.Unauthorized) return await Salir();
        if (!result.Success)
        {
            AddError(result);
            return View(nameof(Seguridad), new SeguridadCuentaView(false));
        }

        TempData["MfaSetup"] = result.Data?.Secreto;
        return View(nameof(Seguridad), new SeguridadCuentaView(false, result.Data?.Secreto));
    }

    [HttpPost]
    public async Task<IActionResult> ActivarMfa(string? codigo, CancellationToken ct)
    {
        var result = await Post<MfaChange>("/mfa/activar", new { codigo }, ct, CurrentJwt());
        if (result.Status == HttpStatusCode.Unauthorized) return await Salir();
        if (!result.Success)
        {
            AddError(result);
            return View(nameof(Seguridad), new SeguridadCuentaView(false, TempData.Peek("MfaSetup") as string));
        }
        TempData.Remove("MfaSetup");
        StoreUpdatedJwt(result.Data);
        return View(nameof(Seguridad), new SeguridadCuentaView(true, Codigos: result.Data?.Codigos));
    }

    [HttpPost]
    public async Task<IActionResult> DesactivarMfa(string? password, string? codigo, CancellationToken ct)
    {
        var result = await Post<MfaChange>("/mfa/desactivar", new { password, codigo }, ct, CurrentJwt());
        if (result.Status == HttpStatusCode.Unauthorized) return await Salir();
        if (!result.Success)
        {
            AddError(result);
            return View(nameof(Seguridad), new SeguridadCuentaView(true));
        }
        StoreUpdatedJwt(result.Data);
        return RedirectToAction(nameof(Seguridad));
    }

    private void StoreUpdatedJwt(MfaChange? data)
    {
        var sessionId = User.FindFirstValue("session_id");
        if (sessionId is not null && !string.IsNullOrWhiteSpace(data?.Jwt))
            tokenStore.Update(sessionId, data.Jwt);
    }

    private string? CurrentJwt()
    {
        var sessionId = User.FindFirstValue("session_id");
        return sessionId is not null && tokenStore.TryGet(sessionId, out var jwt) ? jwt : null;
    }

    private Task<AuthApiResponse<T>> Post<T>(string path, object body, CancellationToken ct, string? jwt = null) =>
        auth.SendAsync<T>(HttpMethod.Post, path, body, ct, jwt);

    private void AddError<T>(AuthApiResponse<T> result)
    {
        if (result.Status == HttpStatusCode.TooManyRequests)
        {
            Response.StatusCode = 429;
            if (result.RetryAfter is { } seconds)
                Response.Headers.RetryAfter = seconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        ModelState.AddModelError(string.Empty, result.Message ?? "El servicio no está disponible. Inténtalo más tarde.");
    }

    private static bool ValidToken(string? token) =>
        token?.Length == 43 && token.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');


}
