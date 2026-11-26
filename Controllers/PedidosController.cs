using System.Net;
using System.Text.Json;
using EcommerceFrontend.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceFrontend.Controllers;

[Authorize(Roles = "Usuario,Administrador")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("pedidos")]
public class PedidosController(
    ComprasClientService compras,
    IAccessTokenStore tokenStore,
    ILogger<PedidosController> logger) : Controller
{
    [HttpGet("")]
    public Task<IActionResult> Index(CancellationToken cancellationToken) =>
        CargarAsync(() => compras.ObtenerPedidosAsync(cancellationToken), nameof(Index), cancellationToken);

    [HttpGet("detalle/{id:int:min(1)}")]
    public Task<IActionResult> Detalle(int id, CancellationToken cancellationToken) =>
        CargarAsync(() => compras.ObtenerPedidoAsync(id, cancellationToken), nameof(Detalle), cancellationToken);

    private async Task<IActionResult> CargarAsync<T>(
        Func<Task<T>> cargar, string vista, CancellationToken cancellationToken)
    {
        try
        {
            return View(vista, await cargar());
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
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.NotFound && vista == nameof(Detalle))
        {
            return NotFound("El pedido no está disponible.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(504, "El servicio tardó demasiado en consultar los pedidos.");
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            logger.LogError(ex, "Error al consultar pedidos.");
            return StatusCode(502, "No fue posible consultar los pedidos.");
        }
    }
}
