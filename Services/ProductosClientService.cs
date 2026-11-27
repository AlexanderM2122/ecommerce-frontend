
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using EcommerceFrontend.Models;
using Microsoft.AspNetCore.Http;

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
        using var contenido = CrearContenido(
            producto.Titulo, producto.Descripcion, producto.Precio, producto.ArchivoId, producto.Imagen);
        using var response = await client.PostAsync("api/productos", contenido, cancellationToken);

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

        using var contenido = CrearContenido(
            producto.Titulo, producto.Descripcion, producto.Precio, archivoId, producto.Imagen);
        using var response = await client.PutAsync($"api/productos/{id}", contenido, cancellationToken);

        response.EnsureSuccessStatusCode();
    }


    private static HttpContent CrearContenido(
        string titulo, string descripcion, decimal precio, int? archivoId, IFormFile? imagen)
    {
        titulo = titulo.Trim();
        descripcion = descripcion.Trim();

        if (imagen is null)
            return JsonContent.Create(new { titulo, descripcion, precio, archivoid = archivoId });

        var formulario = new MultipartFormDataContent();
        formulario.Add(new StringContent(titulo), "titulo");
        formulario.Add(new StringContent(descripcion), "descripcion");
        formulario.Add(new StringContent(precio.ToString(CultureInfo.InvariantCulture)), "precio");
        ArchivosClientService.AgregarImagen(formulario, imagen);
        return formulario;
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
