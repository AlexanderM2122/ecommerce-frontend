
using System.Security.Claims;
using EcommerceFrontend.Models;
using EcommerceFrontend.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceFrontend.Controllers;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AuthController(
    AuthClientService auth,
    IAccessTokenStore tokenStore,
    ILogger<AuthController> logger) : Controller
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View(new Login());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(Login model)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        if (!ModelState.IsValid)
            return View(model);

        AuthUser? usuario;

        try
        {
            usuario = await auth.IniciarSesionAsync(model);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Error de comunicación con la API.");
            ModelState.AddModelError(
                string.Empty,
                "El servicio no está disponible.");
            return View(model);
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Tiempo de espera agotado.");
            ModelState.AddModelError(
                string.Empty,
                "El servicio tardó demasiado en responder.");
            return View(model);
        }

        if (usuario is null)
        {
            ModelState.AddModelError(
                string.Empty,
                "Correo o contraseña incorrectos.");
            return View(model);
        }

        if (string.IsNullOrWhiteSpace(usuario.Email) ||
            string.IsNullOrWhiteSpace(usuario.Nombre) ||
            usuario.Email != model.Email ||
            (usuario.Rol != "Administrador" &&
             usuario.Rol != "Usuario"))
        {
            ModelState.AddModelError(
                string.Empty,
                "La respuesta del servidor no es válida.");
            return View(model);
        }

        var sessionId = tokenStore.Create(usuario.Jwt);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, usuario.Email),
            new(ClaimTypes.GivenName, usuario.Nombre),
            new(ClaimTypes.Role, usuario.Rol),
            new("session_id", sessionId)
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);

        try
        {
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
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
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Salir()
    {
        var sessionId = User.FindFirst("session_id")?.Value;

        if (sessionId is not null)
            tokenStore.Remove(sessionId);

        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction("Index", "Auth");
    }
}
