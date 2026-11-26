using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace EcommerceFrontend.Services;

public record AuthApiResponse<T>(
    HttpStatusCode Status, T? Data = default, string? Message = null, int? RetryAfter = null)
{
    public bool Success => (int)Status is >= 200 and < 300;
}

public class AuthClientService(HttpClient client, ILogger<AuthClientService> logger)
{
    public async Task<AuthApiResponse<T>> SendAsync<T>(
        HttpMethod method, string path, object? body, CancellationToken cancellationToken, string? jwt = null)
    {
        using var request = new HttpRequestMessage(method, "api/auth" + path);
        if (body is not null)
            request.Content = JsonContent.Create(body);
        if (jwt is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                if (typeof(T) == typeof(object))
                    return new(response.StatusCode);

                var data = await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
                return new(response.StatusCode, data);
            }

            string? message = null;
            if ((int)response.StatusCode < 500 && response.Content.Headers.ContentType?.MediaType == "application/json")
            {
                var error = await response.Content.ReadFromJsonAsync<AuthError>(cancellationToken: cancellationToken);
                message = error?.Mensaje;
            }
            var retry = response.Headers.RetryAfter?.Delta?.TotalSeconds;
            return new(response.StatusCode, Message: message, RetryAfter: retry is null ? null : (int)Math.Ceiling(retry.Value));
        }
        catch (Exception ex) when (
            ex is HttpRequestException or JsonException or NotSupportedException ||
            ex is OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            // Los errores de serializaci�n pueden contener datos del segundo factor.
            logger.LogWarning("Error de comunicaci�n con la API de autenticaci�n ({Tipo}).", ex.GetType().Name);
            return new(HttpStatusCode.ServiceUnavailable);
        }
    }

    private record AuthError(string? Mensaje);
}

public record MfaState(bool Activo);
public record MfaSetup(string Secreto);
public record MfaChange(string Jwt, string[]? Codigos);
