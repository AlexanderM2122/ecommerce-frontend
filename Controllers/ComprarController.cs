using System.Net;
using EcommerceFrontend.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceFrontend.Controllers;

[Authorize(Roles = "Usuario,Administrador")]
[Route("comprar")]
public class ComprarController(
    ProductosClientService productos,
    IAccessTokenStore tokenStore,
    ILogger<ComprarController> logger) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(
        [FromQuery] string? s,
        CancellationToken cancellationToken)
    {
        try
        {
            var lista = await productos.GetAsync(s, cancellationToken);
            ViewData["Busqueda"] = s;
            return View(lista);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            var sessionId = User.FindFirst("session_id")?.Value;
            if (sessionId is not null) tokenStore.Remove(sessionId);
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Auth");
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error al cargar catálogo de compra.");
            return StatusCode(502, "No fue posible cargar el catálogo.");
        }
    }
}