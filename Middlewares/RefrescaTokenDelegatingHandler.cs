using EcommerceFrontend.Services;

namespace EcommerceFrontend.Middlewares;

public sealed class RefrescaTokenDelegatingHandler(
    IHttpContextAccessor httpContextAccessor,
    IAccessTokenStore tokenStore) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode &&
            response.Headers.TryGetValues("Set-Authorization", out var valores))
        {
            var nuevoJwt = valores.FirstOrDefault();
            var sessionId = httpContextAccessor.HttpContext?.User
                .FindFirst("session_id")?.Value;

            if (!string.IsNullOrWhiteSpace(nuevoJwt) &&
                !string.IsNullOrWhiteSpace(sessionId))
            {
                tokenStore.Update(sessionId, nuevoJwt);
            }
        }

        return response;
    }
}