
using System.Net;
using EcommerceFrontend.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EcommerceFrontend.Models;
using System.Text.Json;

namespace EcommerceFrontend.Controllers;

[Authorize(Roles = "Usuario,Administrador")]
[Route("productos")]
public class ProductosController(
    ProductosClientService productos,
    CategoriasClientService categorias,
    ArchivosClientService archivos,
    IAccessTokenStore tokenStore,
    ILogger<ProductosController> logger) : BackendControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> Index(
        [FromQuery] string? s,
        CancellationToken cancellationToken)
    {
        if (s?.Length > 100)
            return BadRequest("La búsqueda es demasiado larga.");

        try
        {
            var lista = await productos.GetAsync(
                s, cancellationToken);

            ViewData["Busqueda"] = s;

            return View(lista);
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
        catch (OperationCanceledException ex)
            when (!cancellationToken.IsCancellationRequested)
        {
            return ServicioNoDisponible(
                logger, "consultar los pedidos", ex);
        }
        catch (Exception ex)
            when (ex is HttpRequestException or JsonException)
        {
            return ServicioNoDisponible(
                logger, "consultar los pedidos", ex);
        }
    }


    [HttpGet("detalle/{id:int}")]
    public async Task<IActionResult> Detalle(
        int id,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
            return NotFound();

        try
        {
            var producto = await productos.GetByIdAsync(
                id, cancellationToken);

            if (producto is null)
                return NotFound();

            return View(producto);
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
            logger.LogError(ex, "Error al consultar producto.");

            return StatusCode(502,
                "No fue posible consultar el producto.");
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(504,
                "El backend tardó demasiado en responder.");
        }
    }


    // GET /productos/crear
    [HttpGet("crear")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Crear(
        CancellationToken cancellationToken)
    {
        try
        {
            
            var lista = await categorias.GetAsync(cancellationToken);
            var imagenes = await archivos.GetAsync(cancellationToken);

            return View(new ProductoCrearForm
            {
                CategoriasDisponibles = lista.ToList(),
                ArchivosDisponibles = imagenes.ToList()
            });

        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            return await CerrarSesionProductoAsync();
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            return Forbid();
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error al cargar categorías.");
            return StatusCode(502,
                "No fue posible cargar las categorías.");
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(504,
                "El backend tardó demasiado en responder.");
        }
    }

    // POST /productos/crear
    [HttpPost("crear")]
    [Authorize(Roles = "Administrador")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Crear(
        ProductoCrearForm model,
        CancellationToken cancellationToken)
    {
        int? productoId = null;

        try
        {
            var disponibles = await categorias.GetAsync(
                cancellationToken);

            model.CategoriasDisponibles = disponibles.ToList();

            var imagenes = await archivos.GetAsync(cancellationToken);
            model.ArchivosDisponibles = imagenes.ToList();

            var seleccionadas = model.CategoriaIds?
                .Distinct()
                .ToList() ?? [];

            if (string.IsNullOrWhiteSpace(model.Titulo))
            {
                ModelState.AddModelError(
                    nameof(model.Titulo),
                    "El título es obligatorio.");
            }

            if (string.IsNullOrWhiteSpace(model.Descripcion))
            {
                ModelState.AddModelError(
                    nameof(model.Descripcion),
                    "La descripción es obligatoria.");
            }

            if (decimal.Round(model.Precio, 2) != model.Precio)
            {
                ModelState.AddModelError(
                    nameof(model.Precio),
                    "El precio admite máximo dos decimales.");
            }

            if (seleccionadas.Any(id =>
                !disponibles.Any(c => c.CategoriaId == id)))
            {
                ModelState.AddModelError(
                    nameof(model.CategoriaIds),
                    "Una categoría seleccionada no es válida.");
            }

            if (model.Imagen is null && model.ArchivoId.HasValue &&
                !imagenes.Any(a => a.ArchivoId == model.ArchivoId.Value))
            {
                ModelState.AddModelError(
                    nameof(model.ArchivoId),
                    "La imagen seleccionada no existe.");
            }

            if (!ModelState.IsValid)
                return View(model);

            // Crear el producto en Node.js.
            productoId = await productos.CrearAsync(
                model, cancellationToken);

            // Asociar las categorías seleccionadas.
            foreach (var categoriaId in seleccionadas)
            {
                await productos.AsignarCategoriaAsync(
                    productoId.Value,
                    categoriaId,
                    cancellationToken);
            }

            TempData["Mensaje"] =
                "Producto creado correctamente.";

            return RedirectToAction(
                nameof(Detalle), new { id = productoId.Value });
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            if (productoId.HasValue)
                logger.LogWarning(
                    "Sesión expirada al asociar categorías del producto {Id}.",
                    productoId.Value);

            return await CerrarSesionProductoAsync();
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            return Forbid();
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.BadRequest &&
                !productoId.HasValue)
        {
            ModelState.AddModelError(
                string.Empty,
                model.Imagen is null
                    ? "Los datos del producto no son válidos."
                    : "Revisa los datos y vuelve a seleccionar una imagen JPG válida.");

            return View(model);
        }
        catch (Exception ex)
            when (ex is HttpRequestException ||
                ex is TaskCanceledException ||
                ex is JsonException ||
                ex is InvalidOperationException)
        {
            logger.LogError(ex, "Error al crear producto.");

            if (productoId.HasValue)
            {
                TempData["Aviso"] =
                    "El producto fue creado, pero no se pudieron " +
                    "asociar todas las categorías.";

                return RedirectToAction(
                    nameof(Detalle),
                    new { id = productoId.Value });
            }

            return StatusCode(502,
                "No se pudo confirmar la creación. " +
                "Revisa el listado antes de volver a intentarlo.");
        }
    }


// GET /productos/editar/{id}
[HttpGet("editar/{id:int}")]
[Authorize(Roles = "Administrador")]
public async Task<IActionResult> Editar(
    int id,
    CancellationToken cancellationToken)
{
    if (id <= 0)
        return NotFound();

    try
    {
        var producto = await productos.GetByIdAsync(
            id, cancellationToken);

        if (producto is null)
            return NotFound();

        
        var imagenes = await archivos.GetAsync(cancellationToken);

        var model = new ProductoEditarForm
        {
            Titulo = producto.Titulo,
            Descripcion = producto.Descripcion,
            Precio = producto.Precio,
            ArchivoId = producto.ArchivoId,
            ArchivosDisponibles = imagenes.ToList()
        };


        return View(model);
    }
    catch (HttpRequestException ex)
        when (ex.StatusCode == HttpStatusCode.Unauthorized)
    {
        return await CerrarSesionProductoAsync();
    }
    catch (HttpRequestException ex)
        when (ex.StatusCode == HttpStatusCode.Forbidden)
    {
        return Forbid();
    }
    catch (HttpRequestException ex)
    {
        logger.LogError(ex, "Error al consultar producto.");

        return StatusCode(502,
            "No fue posible consultar el producto.");
    }
    catch (TaskCanceledException)
        when (!cancellationToken.IsCancellationRequested)
    {
        return StatusCode(504,
            "El backend tardó demasiado en responder.");
    }
}

    // POST /productos/editar/{id}
    [HttpPost("editar/{id:int}")]
    [Authorize(Roles = "Administrador")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Editar(
        int id,
        ProductoEditarForm model,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
            return NotFound();

        if (string.IsNullOrWhiteSpace(model.Titulo))
        {
            ModelState.AddModelError(
                nameof(model.Titulo),
                "El título es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(model.Descripcion))
        {
            ModelState.AddModelError(
                nameof(model.Descripcion),
                "La descripción es obligatoria.");
        }

        if (decimal.Round(model.Precio, 2) != model.Precio)
        {
            ModelState.AddModelError(
                nameof(model.Precio),
                "El precio admite máximo dos decimales.");
        }

        try
        {
            
            var productoActual = await productos.GetByIdAsync(
                id, cancellationToken);

            if (productoActual is null)
                return NotFound();

            var imagenes = await archivos.GetAsync(cancellationToken);
            model.ArchivosDisponibles = imagenes.ToList();

            if (model.Imagen is null && model.ArchivoId.HasValue &&
                !imagenes.Any(a => a.ArchivoId == model.ArchivoId.Value))
            {
                ModelState.AddModelError(
                    nameof(model.ArchivoId),
                    "La imagen seleccionada no existe.");
            }

            if (!ModelState.IsValid)
                return View(model);

            await productos.EditarAsync(
                id,
                model,
                model.ArchivoId,
                cancellationToken);


            TempData["Mensaje"] =
                "Producto actualizado correctamente.";

            return RedirectToAction(
                nameof(Detalle), new { id });
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.BadRequest)
        {
            ModelState.AddModelError(
                string.Empty,
                model.Imagen is null
                    ? "Los datos del producto no son válidos."
                    : "Revisa los datos y vuelve a seleccionar una imagen JPG válida.");

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
            return await CerrarSesionProductoAsync();
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            return Forbid();
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error al actualizar producto.");

            return StatusCode(502,
                "No fue posible confirmar la actualización. " +
                "Consulta el producto antes de volver a intentarlo.");
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(504,
                "No se pudo confirmar la actualización. " +
                "Consulta el producto antes de volver a intentarlo.");
        }
    }


// GET /productos/categorias/{id}
[HttpGet("categorias/{id:int}")]
[Authorize(Roles = "Administrador")]
public async Task<IActionResult> Categorias(
    int id,
    CancellationToken cancellationToken)
{
    if (id <= 0)
        return NotFound();

    try
    {
        var producto = await productos.GetByIdAsync(
            id, cancellationToken);

        if (producto is null)
            return NotFound();

        return View(producto);
    }
    catch (HttpRequestException ex)
        when (ex.StatusCode == HttpStatusCode.Unauthorized)
    {
        return await CerrarSesionProductoAsync();
    }
    catch (HttpRequestException ex)
        when (ex.StatusCode == HttpStatusCode.Forbidden)
    {
        return Forbid();
    }
    catch (HttpRequestException ex)
    {
        logger.LogError(ex, "Error al consultar categorías del producto.");
        return StatusCode(502, "No fue posible consultar las categorías.");
    }
    catch (TaskCanceledException)
        when (!cancellationToken.IsCancellationRequested)
    {
        return StatusCode(504, "El backend tardó demasiado en responder.");
    }
}

// GET /productos/categorias/agregar/{id}
[HttpGet("categorias/agregar/{id:int}")]
[Authorize(Roles = "Administrador")]
public async Task<IActionResult> CategoriasAgregar(
    int id,
    CancellationToken cancellationToken)
{
    if (id <= 0)
        return NotFound();

    try
    {
        var producto = await productos.GetByIdAsync(
            id, cancellationToken);

        if (producto is null)
            return NotFound();

        var lista = await categorias.GetAsync(cancellationToken);

        var disponibles = lista
            .Where(c => !producto.Categorias.Any(
                pc => pc.CategoriaId == c.CategoriaId))
            .ToList();

        return View(new ProductoCategoriaAgregarForm
        {
            Producto = producto,
            CategoriasDisponibles = disponibles
        });
    }
    catch (HttpRequestException ex)
        when (ex.StatusCode == HttpStatusCode.Unauthorized)
    {
        return await CerrarSesionProductoAsync();
    }
    catch (HttpRequestException ex)
        when (ex.StatusCode == HttpStatusCode.Forbidden)
    {
        return Forbid();
    }
    catch (HttpRequestException ex)
    {
        logger.LogError(ex, "Error al cargar categorías.");
        return StatusCode(502, "No fue posible cargar las categorías.");
    }
    catch (TaskCanceledException)
        when (!cancellationToken.IsCancellationRequested)
    {
        return StatusCode(504, "El backend tardó demasiado en responder.");
    }
}

    // POST /productos/categorias/agregar/{id}
    [HttpPost("categorias/agregar/{id:int}")]
    [Authorize(Roles = "Administrador")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CategoriasAgregar(
        int id,
        ProductoCategoriaAgregarForm model,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
            return NotFound();

        try
        {
            var producto = await productos.GetByIdAsync(
                id, cancellationToken);

            if (producto is null)
                return NotFound();

            var lista = await categorias.GetAsync(cancellationToken);

            var disponibles = lista
                .Where(c => !producto.Categorias.Any(
                    pc => pc.CategoriaId == c.CategoriaId))
                .ToList();

            model.Producto = producto;
            model.CategoriasDisponibles = disponibles;

            if (!disponibles.Any(
                c => c.CategoriaId == model.CategoriaId))
            {
                ModelState.AddModelError(
                    nameof(model.CategoriaId),
                    "La categoría seleccionada no está disponible.");
            }

            if (!ModelState.IsValid)
                return View(model);

            await productos.AsignarCategoriaAsync(
                id, model.CategoriaId, cancellationToken);

            TempData["Mensaje"] = "Categoría agregada correctamente.";

            return RedirectToAction(nameof(Categorias), new { id });
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            return await CerrarSesionProductoAsync();
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
            when (ex.StatusCode == HttpStatusCode.BadRequest)
        {
            ModelState.AddModelError(
                string.Empty,
                "No fue posible agregar la categoría.");

            return View(model);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error al asignar categoría.");
            return StatusCode(502,
                "No se pudo confirmar la operación. Consulta las categorías antes de reintentar.");
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(504,
                "No se pudo confirmar la operación. Consulta las categorías antes de reintentar.");
        }
    }


    // POST /productos/categorias/remover/{id}/{categoriaId}
    [HttpPost("categorias/remover/{id:int}/{categoriaId:int}")]
    [Authorize(Roles = "Administrador")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CategoriasRemover(
        int id,
        int categoriaId,
        CancellationToken cancellationToken)
    {
        if (id <= 0 || categoriaId <= 0)
            return NotFound();

        try
        {
            var producto = await productos.GetByIdAsync(
                id, cancellationToken);

            if (producto is null)
                return NotFound();

            if (!producto.Categorias.Any(
                c => c.CategoriaId == categoriaId))
            {
                return NotFound();
            }

            await productos.RemoverCategoriaAsync(
                id, categoriaId, cancellationToken);

            TempData["Mensaje"] =
                "Categoría retirada correctamente.";

            return RedirectToAction(nameof(Categorias), new { id });
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            return await CerrarSesionProductoAsync();
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
        {
            logger.LogError(
                ex, "Error al retirar categoría del producto.");

            TempData["Error"] =
                "No se pudo confirmar la operación. " +
                "Revisa las categorías antes de intentarlo nuevamente.";

            return RedirectToAction(nameof(Categorias), new { id });
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            TempData["Error"] =
                "El backend tardó demasiado en responder. " +
                "Revisa las categorías antes de intentarlo nuevamente.";

            return RedirectToAction(nameof(Categorias), new { id });
        }
    }


    // GET /productos/eliminar/{id}
    [HttpGet("eliminar/{id:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Eliminar(
        int id,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
            return NotFound();

        try
        {
            var producto = await productos.GetByIdAsync(
                id, cancellationToken);

            if (producto is null)
                return NotFound();

            return View(producto);
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            return await CerrarSesionProductoAsync();
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            return Forbid();
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error al consultar producto.");
            return StatusCode(502,
                "No fue posible consultar el producto.");
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(504,
                "El backend tardó demasiado en responder.");
        }
    }

    // POST /productos/eliminar/{id}
    [HttpPost("eliminar/{id:int}")]
    [Authorize(Roles = "Administrador")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarConfirmado(
        int id,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
            return NotFound();

        try
        {
            await productos.EliminarAsync(
                id, cancellationToken);

            TempData["Mensaje"] =
                "Producto eliminado correctamente.";

            return RedirectToAction(nameof(Index));
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            return await CerrarSesionProductoAsync();
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
                "No se puede eliminar el producto debido " +
                "a una restricción de la base de datos.";

            return RedirectToAction(nameof(Index));
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error al eliminar producto.");

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



    // Cerrar sesión si el backend rechaza el JWT.
    private async Task<IActionResult> CerrarSesionProductoAsync()
    {
        var sessionId = User.FindFirst("session_id")?.Value;

        if (sessionId is not null)
            tokenStore.Remove(sessionId);

        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction("Index", "Auth");
    }

}
