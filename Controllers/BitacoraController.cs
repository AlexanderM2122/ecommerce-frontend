using System.Net;
using System.Text.Json;
using EcommerceFrontend.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceFrontend.Controllers;

[Authorize(Roles = "Administrador")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("bitacora")]
public class BitacoraController(
    BitacoraClientService bitacora,
    IAccessTokenStore tokenStore,
    ILogger<BitacoraController> logger) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            return View(await bitacora.GetAsync(cancellationToken));
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            var sessionId = User.FindFirst("session_id")?.Value;
            if (sessionId is not null)
                tokenStore.Remove(sessionId);

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Auth");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            return Forbid();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(504, "El servicio tardó demasiado en consultar la bitácora.");
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            logger.LogError(ex, "Error al consultar la bitácora.");
            return StatusCode(502, "No fue posible consultar la bitácora.");
        }
    }
}
