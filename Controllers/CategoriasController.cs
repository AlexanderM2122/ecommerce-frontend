
using System.Net;
using EcommerceFrontend.Models;
using EcommerceFrontend.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceFrontend.Controllers;

[Authorize(Roles = "Administrador")]
[Route("categorias")]
public class CategoriasController(
    CategoriasClientService categorias,
    IAccessTokenStore tokenStore,
    ILogger<CategoriasController> logger) : Controller
{
    // GET /categorias
    [HttpGet("")]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        try
        {
            var lista = await categorias.GetAsync(cancellationToken);
            return View(lista);
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            return await CerrarSesionAsync();
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            return Forbid();
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error al consultar categorías.");

            return StatusCode(502,
                "No fue posible consultar las categorías.");
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(504,
                "El backend tardó demasiado en responder.");
        }
    }

    // GET /categorias/crear
    [HttpGet("crear")]
    public IActionResult Crear()
    {
        return View(new CategoriaForm());
    }

    // POST /categorias/crear
    [HttpPost("crear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(
        CategoriaForm model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await categorias.CrearAsync(model, cancellationToken);

            return RedirectToAction(nameof(Index));
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.BadRequest)
        {
            ModelState.AddModelError(
                string.Empty,
                "Los datos de la categoría no son válidos.");

            return View(model);
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            return await CerrarSesionAsync();
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            return Forbid();
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error al crear categoría.");

            return StatusCode(502,
                "No fue posible crear la categoría.");
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(504,
                "El backend tardó demasiado en responder.");
        }
    }

    // Cerrar sesión cuando el backend rechaza el JWT.
    private async Task<IActionResult> CerrarSesionAsync()
    {
        var sessionId = User.FindFirst("session_id")?.Value;

        if (sessionId is not null)
            tokenStore.Remove(sessionId);

        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction("Index", "Auth");
    }


[HttpGet("editar/{id:int}")]
public async Task<IActionResult> Editar(
    int id,
    CancellationToken cancellationToken)
{
    if (id <= 0)
        return NotFound();

    try
    {
        var categoria = await categorias.GetAsync(
            id, cancellationToken);

        if (categoria is null)
            return NotFound();

        var model = new CategoriaForm
        {
            Nombre = categoria.Nombre
        };

        return View(model);
    }
    catch (HttpRequestException ex)
        when (ex.StatusCode == HttpStatusCode.Unauthorized)
    {
        return await CerrarSesionAsync();
    }
    catch (HttpRequestException ex)
        when (ex.StatusCode == HttpStatusCode.Forbidden)
    {
        return Forbid();
    }
    catch (HttpRequestException ex)
    {
        logger.LogError(ex, "Error al consultar categoría.");

        return StatusCode(502,
            "No fue posible consultar la categoría.");
    }
    catch (TaskCanceledException)
        when (!cancellationToken.IsCancellationRequested)
    {
        return StatusCode(504,
            "El backend tardó demasiado en responder.");
    }
}

    [HttpPost("editar/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(
        int id,
        CategoriaForm model,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
            return NotFound();

        if (string.IsNullOrWhiteSpace(model.Nombre))
        {
            ModelState.AddModelError(
                nameof(model.Nombre),
                "El nombre es obligatorio.");
        }

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await categorias.EditarAsync(
                id, model, cancellationToken);

            return RedirectToAction(nameof(Index));
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.BadRequest)
        {
            ModelState.AddModelError(
                string.Empty,
                "Los datos proporcionados no son válidos.");

            return View(model);
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return NotFound();
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            return await CerrarSesionAsync();
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            return Forbid();
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error al editar categoría.");

            return StatusCode(502,
                "No fue posible actualizar la categoría.");
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(504,
                "El backend tardó demasiado en responder.");
        }
    }


    [HttpGet("detalle/{id:int}")]
    public Task<IActionResult> Detalle(int id, CancellationToken cancellationToken) =>
        MostrarCategoriaAsync(id, nameof(Detalle), cancellationToken);

    [HttpGet("eliminar/{id:int}")]
    public Task<IActionResult> Eliminar(int id, CancellationToken cancellationToken) =>
        MostrarCategoriaAsync(id, nameof(Eliminar), cancellationToken);

    private async Task<IActionResult> MostrarCategoriaAsync(
        int id, string vista, CancellationToken cancellationToken)
    {
        if (id <= 0)
            return NotFound();

        try
        {
            var categoria = await categorias.GetAsync(
                id, cancellationToken);

            if (categoria is null)
                return NotFound();

            return View(vista, categoria);
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            return await CerrarSesionAsync();
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            return Forbid();
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error al consultar categoría.");
            return StatusCode(502,
                "No fue posible consultar la categoría.");
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(504,
                "El backend tardó demasiado en responder.");
        }
    }

    // POST /categorias/eliminar/{id}
    [HttpPost("eliminar/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarConfirmado(
        int id,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
            return NotFound();

        try
        {
            await categorias.EliminarAsync(id, cancellationToken);

            TempData["Mensaje"] = "Categoría eliminada correctamente.";

            return RedirectToAction(nameof(Index));
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.BadRequest)
        {
            TempData["Error"] =
                "No se puede eliminar esta categoría porque está protegida.";

            return RedirectToAction(nameof(Index));
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return NotFound();
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            return await CerrarSesionAsync();
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            return Forbid();
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error al eliminar categoría.");

            return StatusCode(502,
                "No fue posible eliminar la categoría.");
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(504,
                "El backend tardó demasiado en responder.");
        }
    }


}
