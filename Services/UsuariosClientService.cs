
using System.Net.Http.Json;
using EcommerceFrontend.Models;
using System.Net;

namespace EcommerceFrontend.Services;

public sealed class UsuariosClientService(HttpClient client)
{
    public async Task<IReadOnlyList<Usuario>> GetAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await client.GetAsync(
            "api/usuarios",
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<List<Usuario>>(
                cancellationToken: cancellationToken) ?? [];
    }


    public async Task<Usuario?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException(
                "El correo es obligatorio.", nameof(email));

        var emailCodificado = Uri.EscapeDataString(email);

        using var response = await client.GetAsync(
            $"api/usuarios/{emailCodificado}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<Usuario>(
                cancellationToken: cancellationToken);
    }


    public async Task CrearAsync(
        UsuarioCrearForm usuario,
        CancellationToken cancellationToken = default)
    {
        var datos = new
        {
            email = usuario.Email.Trim(),
            nombre = usuario.Nombre.Trim(),
            password = usuario.Password,
            rol = usuario.Rol
        };

        using var response = await client.PostAsJsonAsync(
            "api/usuarios",
            datos,
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    public async Task EditarAsync(
        string email,
        UsuarioEditarForm usuario,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException(
                "El correo es obligatorio.", nameof(email));

        var emailCodificado = Uri.EscapeDataString(email);

        var datos = new
        {
            nombre = usuario.Nombre.Trim(),
            rol = usuario.Rol
        };

        using var response = await client.PutAsJsonAsync(
            $"api/usuarios/{emailCodificado}",
            datos,
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    public async Task EliminarAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException(
                "El correo es obligatorio.", nameof(email));

        var emailCodificado = Uri.EscapeDataString(email.Trim());

        using var response = await client.DeleteAsync(
            $"api/usuarios/{emailCodificado}",
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }

}
