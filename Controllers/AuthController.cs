using System.Net;
using System.Security.Claims;
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
public class AuthController(AuthClientService auth, IAccessTokenStore tokenStore) : Controller
{
    private const string Scheme = CookieAuthenticationDefaults.AuthenticationScheme;
    private const string GenericEmailMessage = "Si la cuenta cumple los requisitos, recibir�s un correo con el enlace.";

    [HttpGet, AllowAnonymous]
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");
        return View(new Login());
    }

    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> Index(Login model, CancellationToken ct)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");
        if (!ModelState.IsValid)
            return View(model);

        var result = await Post<AuthUser>("", model, ct);
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
            TempData["AuthError"] = result.Message ?? "Vuelve a ingresar para iniciar sesi�n.";
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
            ModelState.AddModelError(string.Empty, "La respuesta del servidor no es v�lida.");
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

        var result = await Post<object>("/registro", new { model.Nombre, model.Email, model.Password }, ct);
        if (!result.Success)
        {
            AddError(result);
            return View(model);
        }

        TempData["AuthMessage"] = "Cuenta creada. Revisa tu correo para verificarla.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet, AllowAnonymous]
    public IActionResult ReenviarVerificacion() => EmailView(new(), true);

    [HttpPost, AllowAnonymous]
    public Task<IActionResult> ReenviarVerificacion(CorreoCuentaForm model, CancellationToken ct) =>
        RequestEmail(model, true, ct);

    [HttpGet, AllowAnonymous]
    public IActionResult RecuperarPassword() => EmailView(new(), false);

    [HttpPost, AllowAnonymous]
    public Task<IActionResult> RecuperarPassword(CorreoCuentaForm model, CancellationToken ct) =>
        RequestEmail(model, false, ct);

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
        TempData["AuthMessage"] = GenericEmailMessage;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet, AllowAnonymous]
    public IActionResult VerificarCorreo(string? token) =>
        ValidToken(token) ? View(model: token) : InvalidLink();

    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> VerificarCorreo(string? token, CancellationToken ct)
    {
        if (!ValidToken(token))
            return InvalidLink();
        var result = await Post<object>("/verificar-correo", new { token }, ct);
        if (!result.Success)
        {
            AddError(result);
            return View(model: token);
        }
        TempData["AuthMessage"] = "Correo verificado. Ya puedes iniciar sesi�n.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet, AllowAnonymous]
    public IActionResult RestablecerPassword(string? token) =>
        ValidToken(token) ? View(new RestablecerCuentaForm { Token = token! }) : InvalidLink();

    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> RestablecerPassword(RestablecerCuentaForm model, CancellationToken ct)
    {
        if (!ValidToken(model.Token))
            return InvalidLink();
        if (!ModelState.IsValid)
            return View(model);
        var result = await Post<object>("/restablecer-password", new { model.Token, model.Password }, ct);
        if (!result.Success)
        {
            AddError(result);
            return View(model);
        }
        var sessionId = User.FindFirstValue("session_id");
        if (sessionId is not null) tokenStore.Remove(sessionId);
        TempData.Remove("MfaSetup");
        await HttpContext.SignOutAsync(Scheme);
        TempData["AuthMessage"] = "Contrase�a actualizada. Inicia sesi�n con tu contrase�a nueva.";
        return RedirectToAction(nameof(Index));
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
        ModelState.AddModelError(string.Empty, result.Message ?? "El servicio no est� disponible. Int�ntalo m�s tarde.");
    }

    private IActionResult InvalidLink()
    {
        TempData["AuthError"] = "El enlace es inv�lido, venci� o ya fue utilizado.";
        return RedirectToAction(nameof(Index));
    }

    private static bool ValidToken(string? token) =>
        token?.Length == 43 && token.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');


}
