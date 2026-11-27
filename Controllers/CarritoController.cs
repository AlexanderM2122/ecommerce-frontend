using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
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
[Route("carrito")]
public class CarritoController(
    ComprasClientService compras,
    IAccessTokenStore tokenStore,
    ILogger<CarritoController> logger) : BackendControllerBase
{
    private static readonly ConcurrentDictionary<string, byte> CambiosEnCurso = new();

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            return View(await compras.ObtenerCarritoAsync(cancellationToken));
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            return await CerrarSesionAsync();
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            return Forbid();
        }
        catch (OperationCanceledException ex)
            when (!cancellationToken.IsCancellationRequested)
        {
            return ServicioNoDisponible(
                logger, "cargar el carrito", ex);
        }
        catch (Exception ex)
            when (ex is HttpRequestException or JsonException)
        {
            return ServicioNoDisponible(
                logger, "cargar el carrito", ex);
        }
    }

    [HttpPost("agregar")]
    public Task<IActionResult> Agregar(
        [FromForm, Range(1, int.MaxValue)] int productoId,
        [FromForm, Range(1, 99)] int cantidad) =>
        CambiarAsync(
            () => compras.AgregarAsync(productoId, cantidad),
            "Producto agregado al carrito.",
            "El producto seleccionado ya no está disponible.",
            "Comprar");

    [HttpPost("actualizar")]
    public Task<IActionResult> Actualizar(
        [FromForm, Range(1, int.MaxValue)] int productoId,
        [FromForm, Range(1, 99)] int cantidad) =>
        CambiarAsync(
            () => compras.ActualizarAsync(productoId, cantidad),
            "Cantidad actualizada.",
            "El producto ya no está en el carrito.");

    [HttpPost("eliminar")]
    public Task<IActionResult> Eliminar([FromForm, Range(1, int.MaxValue)] int productoId) =>
        CambiarAsync(
            () => compras.EliminarAsync(productoId),
            "Producto retirado del carrito.",
            "El producto ya no está en el carrito.");

    [HttpPost("confirmar")]
    public Task<IActionResult> Confirmar() =>
        EjecutarCambioAsync(async () =>
        {
            var pedido = await compras.ConfirmarCompraAsync();
            TempData["Mensaje"] = "Compra simulada completada.";
            return RedirectToAction("Detalle", "Pedidos", new { id = pedido.Id });
        }, "Carrito", "No se pudo confirmar la compra. Revisa tus pedidos antes de intentarlo de nuevo.");

    private Task<IActionResult> CambiarAsync(
        Func<Task> cambiar, string mensaje, string mensajeNoEncontrado, string destino = "Carrito")
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Revisa el producto y la cantidad. Puedes seleccionar de 1 a 99 unidades.";
            return Task.FromResult<IActionResult>(RedirectToAction(nameof(Index), destino));
        }

        return EjecutarCambioAsync(async () =>
        {
            await cambiar();
            TempData["Mensaje"] = mensaje;
            return RedirectToAction(nameof(Index), destino);
        }, destino, "No se pudo confirmar el cambio. Revisa el carrito antes de intentarlo de nuevo.",
            mensajeNoEncontrado);
    }

    private async Task<IActionResult> EjecutarCambioAsync(
        Func<Task<IActionResult>> cambiar, string destino, string mensajeIncierto,
        string? mensajeNoEncontrado = null)
    {
        var usuario = User.Identity?.Name;
        if (string.IsNullOrEmpty(usuario))
            return await CerrarSesionAsync();

        // Un cambio a la vez por usuario, incluso si tiene varias pestañas abiertas.
        if (!CambiosEnCurso.TryAdd(usuario, 0))
        {
            TempData["Error"] = "Ya hay una operación en curso. Espera a que termine y revisa el carrito.";
            return RedirectToAction(nameof(Index), destino);
        }

        try
        {
            return await cambiar();
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            return await CerrarSesionAsync();
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            return Forbid();
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
        {
            TempData["Error"] = ex.Message;
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.NotFound && mensajeNoEncontrado is not null)
        {
            TempData["Error"] = mensajeNoEncontrado;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or OperationCanceledException)
        {
            logger.LogWarning(
                "No se pudo confirmar una operación de compra. Tipo: {Tipo}",
                ex.GetType().Name);
            TempData["Error"] = mensajeIncierto;
        }
        finally
        {
            CambiosEnCurso.TryRemove(usuario, out _);
        }

        return RedirectToAction(nameof(Index), destino);
    }

    private async Task<IActionResult> CerrarSesionAsync()
    {
        var sessionId = User.FindFirst("session_id")?.Value;
        if (sessionId is not null)
            tokenStore.Remove(sessionId);

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Auth");
    }
}
