using System.Net.Http.Json;
using EcommerceFrontend.Models;

namespace EcommerceFrontend.Services;

public sealed class BitacoraClientService(HttpClient client)
{
    public async Task<IReadOnlyList<RegistroBitacora>> GetAsync(CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync("api/bitacora", cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<RegistroBitacora>>(
            cancellationToken: cancellationToken) ?? [];
    }
}
