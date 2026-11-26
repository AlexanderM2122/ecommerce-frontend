using System.Net;
using System.Text.Json;
using EcommerceFrontend.Models;

namespace EcommerceFrontend.Services;

public sealed class ComprasClientService(HttpClient client)
{
    public Task<Carrito> ObtenerCarritoAsync(CancellationToken cancellationToken) =>
        ObtenerAsync<Carrito>("api/carrito", cancellationToken);

    public Task<List<Pedido>> ObtenerPedidosAsync(CancellationToken cancellationToken) =>
        ObtenerAsync<List<Pedido>>("api/pedidos", cancellationToken);

    public Task<Pedido> ObtenerPedidoAsync(int id, CancellationToken cancellationToken) =>
        ObtenerAsync<Pedido>($"api/pedidos/{id}", cancellationToken);

    public async Task AgregarAsync(int productoId, int cantidad)
    {
        using var response = await client.PostAsJsonAsync(
            "api/carrito", new { productoid = productoId, cantidad });
        await ValidarCambioAsync(response);
    }

    public async Task ActualizarAsync(int productoId, int cantidad)
    {
        using var response = await client.PutAsJsonAsync(
            $"api/carrito/{productoId}", new { cantidad });
        await ValidarCambioAsync(response);
    }

    public async Task EliminarAsync(int productoId)
    {
        using var response = await client.DeleteAsync($"api/carrito/{productoId}");
        await ValidarCambioAsync(response);
    }

    public async Task<Pedido> ConfirmarCompraAsync()
    {
        using var response = await client.PostAsync("api/pedidos", content: null);
        await ValidarCambioAsync(response);

        var pedido = await LeerAsync<Pedido>(response);
        if (pedido.Id <= 0)
            throw new JsonException("La API no devolvió un identificador de pedido válido.");

        return pedido;
    }

    private async Task<T> ObtenerAsync<T>(string url, CancellationToken cancellationToken)
        where T : class
    {
        using var response = await client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await LeerAsync<T>(response, cancellationToken);
    }

    private static async Task<T> LeerAsync<T>(
        HttpResponseMessage response, CancellationToken cancellationToken = default)
        where T : class =>
        await response.Content.ReadFromJsonAsync<T>(cancellationToken)
        ?? throw new JsonException("La API devolvió una respuesta vacía.");

    private static async Task ValidarCambioAsync(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            string? mensaje = null;

            try
            {
                var error = await response.Content.ReadFromJsonAsync<ErrorApi>();
                mensaje = error?.Mensaje;
            }
            catch (JsonException)
            {
                // Algunas respuestas de error pueden llegar sin un cuerpo JSON.
            }

            throw new HttpRequestException(
                string.IsNullOrWhiteSpace(mensaje) ? "La API rechazó la operación." : mensaje,
                null,
                response.StatusCode);
        }

        response.EnsureSuccessStatusCode();
    }

    private sealed record ErrorApi(string? Mensaje);
}
