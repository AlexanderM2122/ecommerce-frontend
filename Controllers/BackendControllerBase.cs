using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceFrontend.Controllers;

public abstract class BackendControllerBase : Controller
{
    protected IActionResult ServicioNoDisponible(
        ILogger logger,
        string operacion,
        Exception exception)
    {
        var estado = (exception as HttpRequestException)?.StatusCode;

        logger.LogWarning(
            "No fue posible {Operacion}. Tipo: {Tipo}. Estado HTTP: {Estado}",
            operacion,
            exception.GetType().Name,
            estado);

        Response.StatusCode = exception switch
        {
            OperationCanceledException =>
                StatusCodes.Status504GatewayTimeout,

            HttpRequestException httpEx
                when httpEx.StatusCode is null
                    or HttpStatusCode.ServiceUnavailable =>
                StatusCodes.Status503ServiceUnavailable,

            _ => StatusCodes.Status502BadGateway
        };

        Response.Headers.CacheControl = "no-cache, no-store";
        Response.Headers.Pragma = "no-cache";

        return View("ServicioNoDisponible");
    }
}