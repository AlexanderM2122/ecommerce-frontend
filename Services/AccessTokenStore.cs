
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;

namespace EcommerceFrontend.Services;

public interface IAccessTokenStore
{
    string Create(string jwt);
    bool TryGet(string sessionId, out string? jwt);
    void Remove(string sessionId);
}

public sealed class MemoryAccessTokenStore(IMemoryCache cache)
    : IAccessTokenStore
{
    private static readonly TimeSpan Duracion =
        TimeSpan.FromMinutes(19);

    public string Create(string jwt)
    {
        if (string.IsNullOrWhiteSpace(jwt))
            throw new ArgumentException("JWT inválido.");

        var sessionId = Convert.ToHexString(
            RandomNumberGenerator.GetBytes(32));

        cache.Set($"access:{sessionId}", jwt, Duracion);

        return sessionId;
    }

    public bool TryGet(string sessionId, out string? jwt)
    {
        jwt = null;

        if (string.IsNullOrWhiteSpace(sessionId))
            return false;

        return cache.TryGetValue(
            $"access:{sessionId}", out jwt)
            && !string.IsNullOrWhiteSpace(jwt);
    }

    public void Remove(string sessionId)
    {
        if (!string.IsNullOrWhiteSpace(sessionId))
            cache.Remove($"access:{sessionId}");
    }
}
