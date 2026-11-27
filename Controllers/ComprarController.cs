using System.Net;
using EcommerceFrontend.Models;
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
    CategoriasClientService categorias,
    IAccessTokenStore tokenStore,
    ILogger<ComprarController> logger) : BackendControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> Index(
        [FromQuery] string? s,
        [FromQuery] int? categoriaId,
        [FromQuery] string? orden,
        CancellationToken cancellationToken)
    {
        try
        {
            var lista = await productos.GetAsync(s, cancellationToken);
            var opciones = (await categorias.GetAsync(cancellationToken))
                .OrderBy(categoria => categoria.Nombre)
                .ToArray();

            IEnumerable<Producto> resultados = categoriaId is int id
                ? lista.Where(producto => producto.Categorias.Any(categoria => categoria.CategoriaId == id))
                : lista;

            resultados = orden switch
            {
                "precio-asc" => resultados.OrderBy(producto => producto.Precio),
                "precio-desc" => resultados.OrderByDescending(producto => producto.Precio),
                "titulo-asc" => resultados.OrderBy(producto => producto.Titulo),
                "titulo-desc" => resultados.OrderByDescending(producto => producto.Titulo),
                _ => resultados
            };

            return View(new CatalogoCompra(resultados.ToArray(), opciones, s, categoriaId, orden));
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            var sessionId = User.FindFirst("session_id")?.Value;

            if (sessionId is not null)
                tokenStore.Remove(sessionId);

            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Index", "Auth");
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            return Forbid();
        }
        catch (HttpRequestException ex)
        {
            return ServicioNoDisponible(
                logger, "cargar el catálogo", ex);
        }
        catch (OperationCanceledException ex)
            when (!cancellationToken.IsCancellationRequested)
        {
            return ServicioNoDisponible(
                logger, "cargar el catálogo", ex);
        }
    }
}