using System.Net;
using System.Net.Http.Json;
using EcommerceFrontend.Models;

namespace EcommerceFrontend.Services;

public class AuthClientService(HttpClient client)
{
    public async Task<AuthUser?> IniciarSesionAsync(
        Login model,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.PostAsJsonAsync(
            "api/auth",
            model,
            cancellationToken
        );

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var usuario = await response.Content.ReadFromJsonAsync<AuthUser>(
            cancellationToken: cancellationToken
        );

        if (string.IsNullOrWhiteSpace(usuario?.Jwt))
        {
            throw new InvalidOperationException(
                "La respuesta de autenticación no es válida."
            );
        }

        return usuario;
    }

    public async Task<(bool Ok, string? MensajeError)> RegistrarAsync(
        RegistroForm model,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            nombre = model.Nombre.Trim(),
            email = model.Email.Trim(),
            password = model.Password
        };

        using var response = await client.PostAsJsonAsync(
            "api/auth/registro",
            payload,
            cancellationToken
        );

        if (response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.Conflict)
        {
            return (false, "No fue posible crear la cuenta. Verifica tus datos o intenta con otro correo.");
        }

        response.EnsureSuccessStatusCode();
        return (true, null);
    }

}

