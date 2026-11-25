using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceFrontend.Controllers;

[Authorize(Roles = "Usuario,Administrador")]
[Route("carrito")]
public class CarritoController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost("agregar")]
    [ValidateAntiForgeryToken]
    public IActionResult Agregar(
        [Range(1, int.MaxValue)] int productoId,
        [Range(1, 99)] int cantidad)
    {
        if (!ModelState.IsValid)
        {
            TempData["Mensaje"] = "Cantidad o producto inválido.";
            return RedirectToAction("Index", "Comprar");
        }

        // Pendiente: conectar CarritoClientService cuando exista /api/carrito en el backend
        TempData["Mensaje"] = $"Esqueleto listo: producto #{productoId} (x{cantidad}) pendiente de conectar con el backend.";
        return RedirectToAction("Index", "Comprar");
    }
}