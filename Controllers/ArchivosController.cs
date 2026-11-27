
using System.Net;
using EcommerceFrontend.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EcommerceFrontend.Models;

namespace EcommerceFrontend.Controllers;

[Authorize(Roles = "Administrador")]
[Route("archivos")]
public class ArchivosController(
    ArchivosClientService archivos,
    IAccessTokenStore tokenStore,
    ILogger<ArchivosController> logger) : BackendControllerBase
{
    // GET /archivos
    [HttpGet("")]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        try
        {
            var lista = await archivos.GetAsync(
                cancellationToken);

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
            return ServicioNoDisponible(
                logger, "consultar los productos", ex);
        }
        catch (OperationCanceledException ex)
            when (!cancellationToken.IsCancellationRequested)
        {
            return ServicioNoDisponible(
                logger, "consultar los productos", ex);
        }
    }

    // GET /archivos/imagen/{id}
    
    [AllowAnonymous]
    [HttpGet("imagen/{id:int}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]

    public async Task<IActionResult> Imagen(
        int id,
        CancellationToken cancellationToken)
    {

        if (User.Identity?.IsAuthenticated != true)
            return Challenge();

        if (!User.IsInRole("Usuario") &&
            !User.IsInRole("Administrador"))
        {
            return Forbid();
        }

        if (id <= 0)
            return NotFound();

        try
        {
            var contenido = await archivos.ObtenerImagenAsync(
                id, cancellationToken);

            if (contenido is null)
                return NotFound();

            Response.Headers["X-Content-Type-Options"] = "nosniff";

            return File(contenido, "image/jpeg");
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            return Unauthorized();
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            return Forbid();
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error al obtener imagen.");

            return StatusCode(502);
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(504);
        }
    }


    // GET /archivos/subir
    [HttpGet("subir")]
    public IActionResult Subir()
    {
        return View(new ArchivoSubirForm());
    }

    // POST /archivos/subir
    [HttpPost("subir")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Subir(
        ArchivoSubirForm model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        var archivo = model.File;

        if (archivo is null || archivo.Length == 0)
        {
            ModelState.AddModelError(
                nameof(model.File),
                "Selecciona una imagen válida.");

            return View(model);
        }

        if (archivo.Length > 5L * 1024 * 1024)
        {
            ModelState.AddModelError(
                nameof(model.File),
                "La imagen no debe superar los 5 MB.");

            return View(model);
        }

        if (!Path.GetExtension(archivo.FileName)
                .Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(
                archivo.ContentType,
                "image/jpeg",
                StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(
                nameof(model.File),
                "Solamente se permiten imágenes JPG.");

            return View(model);
        }

        // Verificar la firma inicial del archivo JPEG.
        if (archivo.Length < 3)
        {
            ModelState.AddModelError(
                nameof(model.File),
                "El archivo no es una imagen JPG válida.");

            return View(model);
        }

        await using (var lectura = archivo.OpenReadStream())
        {
            var firma = new byte[3];

            await lectura.ReadExactlyAsync(
                firma, cancellationToken);

            if (firma[0] != 0xFF ||
                firma[1] != 0xD8 ||
                firma[2] != 0xFF)
            {
                ModelState.AddModelError(
                    nameof(model.File),
                    "El archivo no tiene una firma JPEG válida.");

                return View(model);
            }
        }

        try
        {
            await archivos.SubirAsync(
                archivo, cancellationToken);

            TempData["Mensaje"] =
                "Imagen subida correctamente.";

            return RedirectToAction(nameof(Index));
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.BadRequest)
        {
            ModelState.AddModelError(
                string.Empty,
                "El backend rechazó la imagen.");

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
            logger.LogError(ex, "Error al subir imagen.");

            return StatusCode(502,
                "No se pudo confirmar la carga. " +
                "Revisa el listado antes de volver a intentarlo.");
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(504,
                "No se pudo confirmar la carga. " +
                "Revisa el listado antes de volver a intentarlo.");
        }
    }

    // GET /archivos/detalle/{id}
    [HttpGet("detalle/{id:int}")]
    public async Task<IActionResult> Detalle(
        int id,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
            return NotFound();

        try
        {
            var archivo = await archivos.GetByIdAsync(
                id, cancellationToken);

            if (archivo is null)
                return NotFound();

            return View(archivo);
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
            logger.LogError(ex, "Error al consultar archivo.");

            return StatusCode(502,
                "No fue posible consultar el archivo.");
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(504,
                "El backend tardó demasiado en responder.");
        }
    }


    // GET /archivos/editar/{id}
    [HttpGet("editar/{id:int}")]
    public async Task<IActionResult> Editar(
        int id,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
            return NotFound();

        try
        {
            var archivo = await archivos.GetByIdAsync(
                id, cancellationToken);

            if (archivo is null)
                return NotFound();

            return View(new ArchivoEditarForm
            {
                Actual = archivo
            });
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
            logger.LogError(ex, "Error al consultar archivo.");

            return StatusCode(502,
                "No fue posible consultar el archivo.");
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(504,
                "El backend tardó demasiado en responder.");
        }
    }

    // POST /archivos/editar/{id}
    [HttpPost("editar/{id:int}")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Editar(
        int id,
        ArchivoEditarForm model,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
            return NotFound();

        try
        {
            var actual = await archivos.GetByIdAsync(
                id, cancellationToken);

            if (actual is null)
                return NotFound();

            model.Actual = actual;

            if (!ModelState.IsValid)
                return View(model);

            var archivo = model.File;

            if (archivo is null || archivo.Length == 0)
            {
                ModelState.AddModelError(
                    nameof(model.File),
                    "Selecciona una imagen válida.");

                return View(model);
            }

            if (archivo.Length > 5L * 1024 * 1024)
            {
                ModelState.AddModelError(
                    nameof(model.File),
                    "La imagen no debe superar los 5 MB.");

                return View(model);
            }

            if (!Path.GetExtension(archivo.FileName)
                    .Equals(".jpg", StringComparison.OrdinalIgnoreCase)
                || !string.Equals(
                    archivo.ContentType,
                    "image/jpeg",
                    StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(
                    nameof(model.File),
                    "Solamente se permiten imágenes JPG.");

                return View(model);
            }

            if (archivo.Length < 3)
            {
                ModelState.AddModelError(
                    nameof(model.File),
                    "El archivo no es una imagen JPG válida.");

                return View(model);
            }

            await using (var lectura = archivo.OpenReadStream())
            {
                var firma = new byte[3];

                await lectura.ReadExactlyAsync(
                    firma, cancellationToken);

                if (firma[0] != 0xFF ||
                    firma[1] != 0xD8 ||
                    firma[2] != 0xFF)
                {
                    ModelState.AddModelError(
                        nameof(model.File),
                        "El archivo no tiene una firma JPEG válida.");

                    return View(model);
                }
            }

            await archivos.ReemplazarAsync(
                id, archivo, cancellationToken);

            TempData["Mensaje"] =
                "Imagen reemplazada correctamente.";

            return RedirectToAction(
                nameof(Detalle), new { id });
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.BadRequest)
        {
            ModelState.AddModelError(
                string.Empty,
                "El backend rechazó la imagen.");

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
            logger.LogError(ex, "Error al reemplazar imagen.");

            return StatusCode(502,
                "No se pudo confirmar el reemplazo. " +
                "Consulta el archivo antes de volver a intentarlo.");
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(504,
                "No se pudo confirmar el reemplazo. " +
                "Consulta el archivo antes de volver a intentarlo.");
        }
    }


    // GET /archivos/eliminar/{id}
    [HttpGet("eliminar/{id:int}")]
    public async Task<IActionResult> Eliminar(
        int id,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
            return NotFound();

        try
        {
            var archivo = await archivos.GetByIdAsync(
                id, cancellationToken);

            if (archivo is null)
                return NotFound();

            return View(archivo);
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
            logger.LogError(ex, "Error al consultar archivo.");

            return StatusCode(502,
                "No fue posible consultar el archivo.");
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(504,
                "El backend tardó demasiado en responder.");
        }
    }

    // POST /archivos/eliminar/{id}
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
            await archivos.EliminarAsync(
                id, cancellationToken);

            TempData["Mensaje"] =
                "Imagen eliminada correctamente.";

            return RedirectToAction(nameof(Index));
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
            when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return NotFound();
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            TempData["Error"] =
                "No se puede eliminar la imagen debido " +
                "a una restricción de la base de datos.";

            return RedirectToAction(nameof(Index));
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error al eliminar imagen.");

            TempData["Error"] =
                "No se pudo confirmar la eliminación. " +
                "Revisa el listado antes de intentarlo nuevamente.";

            return RedirectToAction(nameof(Index));
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            TempData["Error"] =
                "El backend tardó demasiado en responder. " +
                "Revisa el listado antes de intentarlo nuevamente.";

            return RedirectToAction(nameof(Index));
        }
    }

    private async Task<IActionResult> CerrarSesionAsync()
    {
        var sessionId = User.FindFirst("session_id")?.Value;

        if (sessionId is not null)
            tokenStore.Remove(sessionId);

        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction("Index", "Auth");
    }
}
