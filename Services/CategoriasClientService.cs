
using System.Net;
using System.Net.Http.Json;
using EcommerceFrontend.Models;

namespace EcommerceFrontend.Services;

public sealed class CategoriasClientService(HttpClient client)
{
    public async Task<IReadOnlyList<Categoria>> GetAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await client.GetAsync(
            "api/categorias", cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<List<Categoria>>(
                cancellationToken: cancellationToken) ?? [];
    }

    public async Task<Categoria?> GetAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ArgumentOutOfRangeException(nameof(id));

        using var response = await client.GetAsync(
            $"api/categorias/{id}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<Categoria>(
                cancellationToken: cancellationToken);
    }

    public async Task CrearAsync(
        CategoriaForm categoria,
        CancellationToken cancellationToken = default)
    {
        var datos = new
        {
            nombre = categoria.Nombre.Trim()
        };

        using var response = await client.PostAsJsonAsync(
            "api/categorias",
            datos,
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    public async Task EditarAsync(
        int id,
        CategoriaForm categoria,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ArgumentOutOfRangeException(nameof(id));

        var datos = new
        {
            nombre = categoria.Nombre.Trim()
        };

        using var response = await client.PutAsJsonAsync(
            $"api/categorias/{id}",
            datos,
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
            $"api/categorias/{id}",
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }

}
