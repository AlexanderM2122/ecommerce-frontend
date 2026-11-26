
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using EcommerceFrontend.Models;

namespace EcommerceFrontend.Services;

public sealed class ProductosClientService(HttpClient client)
{
    public async Task<IReadOnlyList<Producto>> GetAsync(
        string? busqueda = null,
        CancellationToken cancellationToken = default)
    {
        var url = "api/productos";

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            url += "?s=" + Uri.EscapeDataString(
                busqueda.Trim());
        }

        using var response = await client.GetAsync(
            url, cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<List<Producto>>(
                cancellationToken: cancellationToken) ?? [];
    }

    public async Task<Producto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ArgumentOutOfRangeException(nameof(id));

        using var response = await client.GetAsync(
            $"api/productos/{id}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<Producto>(
                cancellationToken: cancellationToken);
    }

    public async Task<int> CrearAsync(
        ProductoCrearForm producto,
        CancellationToken cancellationToken = default)
    {
        var datos = new
        {
            titulo = producto.Titulo.Trim(),
            descripcion = producto.Descripcion.Trim(),
            precio = producto.Precio,
            archivoid = producto.ArchivoId
        };

        using var response = await client.PostAsJsonAsync(
            "api/productos",
            datos,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var resultado = await response.Content
            .ReadFromJsonAsync<ProductoCreadoResponse>(
                cancellationToken: cancellationToken);

        if (resultado is null || resultado.Id <= 0)
            throw new InvalidOperationException(
                "El backend no devolvió un ID válido.");

        return resultado.Id;
    }

    public async Task AsignarCategoriaAsync(
        int productoId,
        int categoriaId,
        CancellationToken cancellationToken = default)
    {
        if (productoId <= 0 || categoriaId <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(productoId));

        var datos = new
        {
            categoriaid = categoriaId
        };

        using var response = await client.PostAsJsonAsync(
            $"api/productos/{productoId}/categoria",
            datos,
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    private sealed class ProductoCreadoResponse
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
    }


    public async Task EditarAsync(
        int id,
        ProductoEditarForm producto,
        int? archivoId,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ArgumentOutOfRangeException(nameof(id));

        var datos = new
        {
            titulo = producto.Titulo.Trim(),
            descripcion = producto.Descripcion.Trim(),
            precio = producto.Precio,
            archivoid = archivoId
        };

        using var response = await client.PutAsJsonAsync(
            $"api/productos/{id}",
            datos,
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }


    public async Task RemoverCategoriaAsync(
        int productoId,
        int categoriaId,
        CancellationToken cancellationToken = default)
    {
        if (productoId <= 0)
            throw new ArgumentOutOfRangeException(nameof(productoId));

        if (categoriaId <= 0)
            throw new ArgumentOutOfRangeException(nameof(categoriaId));

        using var response = await client.DeleteAsync(
            $"api/productos/{productoId}/categoria/{categoriaId}",
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }


    public async Task EliminarAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ArgumentOutOfRangeException(nameof(id));

        using var response = await client.DeleteAsync(
            $"api/productos/{id}",
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }

}
