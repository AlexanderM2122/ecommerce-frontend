
using System.Net;
using System.Net.Http.Json;
using EcommerceFrontend.Models;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace EcommerceFrontend.Services;

public sealed class ArchivosClientService(HttpClient client)
{
    public async Task<IReadOnlyList<Archivo>> GetAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await client.GetAsync(
            "api/archivos",
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<List<Archivo>>(
                cancellationToken: cancellationToken) ?? [];
    }

    public async Task<byte[]?> ObtenerImagenAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ArgumentOutOfRangeException(nameof(id));

        using var response = await client.GetAsync(
            $"api/archivos/{id}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsByteArrayAsync(
            cancellationToken);
    }


    public async Task SubirAsync(
        IFormFile archivo,
        CancellationToken cancellationToken = default)
    {
        using var formulario = new MultipartFormDataContent();
        AgregarImagen(formulario, archivo);

        using var response = await client.PostAsync(
            "api/archivos",
            formulario,
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }


    internal static void AgregarImagen(MultipartFormDataContent formulario, IFormFile archivo)
    {
        // Normalizar el nombre antes de enviarlo al backend.
        var nombreBase = Path.GetFileNameWithoutExtension(
            archivo.FileName);

        var nombreSeguro = Regex.Replace(
            nombreBase,
            @"[^a-zA-Z0-9_-]",
            "_");

        if (nombreSeguro.Length > 60)
            nombreSeguro = nombreSeguro[..60];

        if (string.IsNullOrWhiteSpace(
            nombreSeguro.Trim('_')))
        {
            nombreSeguro = "imagen";
        }

        var nombreFinal = $"{nombreSeguro}.jpg";

        var contenido = new StreamContent(
            archivo.OpenReadStream());

        contenido.Headers.ContentType =
            new MediaTypeHeaderValue("image/jpeg");

        formulario.Add(
            contenido,
            "file",
            nombreFinal);
    }

    public async Task<Archivo?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ArgumentOutOfRangeException(nameof(id));

        using var response = await client.GetAsync(
            $"api/archivos/{id}/detalle",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<Archivo>(
                cancellationToken: cancellationToken);
    }

    public async Task ReemplazarAsync(
        int id,
        IFormFile archivo,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ArgumentOutOfRangeException(nameof(id));

        var nombreBase = Path.GetFileNameWithoutExtension(
            archivo.FileName);

        var nombreSeguro = Regex.Replace(
            nombreBase,
            @"[^a-zA-Z0-9_-]",
            "_");

        if (nombreSeguro.Length > 60)
            nombreSeguro = nombreSeguro[..60];

        if (string.IsNullOrWhiteSpace(nombreSeguro.Trim('_')))
            nombreSeguro = "imagen";

        var nombreFinal = $"{nombreSeguro}.jpg";

        using var formulario = new MultipartFormDataContent();

        using var contenido = new StreamContent(
            archivo.OpenReadStream());

        contenido.Headers.ContentType =
            new MediaTypeHeaderValue("image/jpeg");

        formulario.Add(
            contenido,
            "file",
            nombreFinal);

        using var response = await client.PutAsync(
            $"api/archivos/{id}",
            formulario,
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
            $"api/archivos/{id}",
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }

}
