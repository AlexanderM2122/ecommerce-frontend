
using System.Net;
using EcommerceFrontend.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EcommerceFrontend.Models;

namespace EcommerceFrontend.Controllers;

[Authorize(Roles = "Administrador")]
[Route("usuarios")]
public class UsuariosController(
    UsuariosClientService usuarios,
    IAccessTokenStore tokenStore,
    ILogger<UsuariosController> logger) : Controller
{
    // GET /usuarios
    [HttpGet("")]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        try
        {
            var lista = await usuarios.GetAsync(
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
            logger.LogError(ex,
                "Error al consultar usuarios.");

            return StatusCode(502,
                "No fue posible consultar los usuarios.");
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(504,
                "El backend tardó demasiado en responder.");
        }
    }


    // GET /usuarios/detalle/{email}
    [HttpGet("detalle/{email}")]
    public async Task<IActionResult> Detalle(
        string email,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
            return NotFound();

        try
        {
            var usuario = await usuarios.GetByEmailAsync(
                email, cancellationToken);

            if (usuario is null)
                return NotFound();

            return View(usuario);
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
            logger.LogError(ex, "Error al consultar usuario.");

            return StatusCode(502,
                "No fue posible consultar el usuario.");
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(504,
                "El backend tardó demasiado en responder.");
        }
    }


    // GET /usuarios/crear
    [HttpGet("crear")]
    public IActionResult Crear()
    {
        return View(new UsuarioCrearForm());
    }

    // POST /usuarios/crear
    [HttpPost("crear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(
        UsuarioCrearForm model,
        CancellationToken cancellationToken)
    {
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
            await usuarios.CrearAsync(
                model, cancellationToken);

            TempData["Mensaje"] =
                "Usuario creado correctamente.";

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
            when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            ModelState.AddModelError(
                nameof(model.Email),
                "Ya existe un usuario con ese correo electrónico.");

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
            logger.LogError(ex, "Error al crear usuario.");

            return StatusCode(502,
                "No se pudo confirmar la creación. " +
                "Revisa el listado antes de intentarlo nuevamente.");
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(504,
                "No se pudo confirmar la creación. " +
                "Revisa el listado antes de intentarlo nuevamente.");
        }
    }


    // GET /usuarios/editar/{email}
    [HttpGet("editar/{email}")]
    public async Task<IActionResult> Editar(
        string email,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
            return NotFound();

        try
        {
            var usuario = await usuarios.GetByEmailAsync(
                email, cancellationToken);

            if (usuario is null)
                return NotFound();

            ViewData["Email"] = usuario.Email;

            return View(new UsuarioEditarForm
            {
                Nombre = usuario.Nombre,
                Rol = usuario.Rol
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
            logger.LogError(ex, "Error al consultar usuario.");

            return StatusCode(502,
                "No fue posible consultar el usuario.");
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(504,
                "El backend tardó demasiado en responder.");
        }
    }

    // POST /usuarios/editar/{email}
    [HttpPost("editar/{email}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(
        string email,
        UsuarioEditarForm model,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
            return NotFound();

        ViewData["Email"] = email;

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
            await usuarios.EditarAsync(
                email, model, cancellationToken);

            TempData["Mensaje"] =
                "Usuario actualizado correctamente.";

            return RedirectToAction(
                nameof(Detalle), new { email });
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
            logger.LogError(ex, "Error al editar usuario.");

            return StatusCode(502,
                "No se pudo confirmar la actualización. " +
                "Consulta el usuario antes de volver a intentarlo.");
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(504,
                "No se pudo confirmar la actualización. " +
                "Consulta el usuario antes de volver a intentarlo.");
        }
    }


    // GET /usuarios/eliminar/{email}
    [HttpGet("eliminar/{email}")]
    public async Task<IActionResult> Eliminar(
        string email,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
            return NotFound();

        try
        {
            var usuario = await usuarios.GetByEmailAsync(
                email, cancellationToken);

            if (usuario is null)
                return NotFound();

            return View(usuario);
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
            logger.LogError(ex, "Error al consultar usuario.");
            return StatusCode(502,
                "No fue posible consultar el usuario.");
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(504,
                "El backend tardó demasiado en responder.");
        }
    }

    // POST /usuarios/eliminar/{email}
    [HttpPost("eliminar/{email}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarConfirmado(
        string email,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
            return NotFound();

        try
        {
            await usuarios.EliminarAsync(
                email, cancellationToken);

            TempData["Mensaje"] =
                "Usuario eliminado correctamente.";

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
            TempData["Error"] =
                "No se puede eliminar este usuario. " +
                "Puede tratarse de una cuenta protegida.";

            return RedirectToAction(nameof(Index));
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
                "No se puede eliminar el usuario debido " +
                "a una restricción de la base de datos.";

            return RedirectToAction(nameof(Index));
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error al eliminar usuario.");

            TempData["Error"] =
                "No se pudo confirmar la eliminación. " +
                "Revisa el listado antes de volver a intentarlo.";

            return RedirectToAction(nameof(Index));
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            TempData["Error"] =
                "El backend tardó demasiado en responder. " +
                "Revisa el listado antes de volver a intentarlo.";

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
