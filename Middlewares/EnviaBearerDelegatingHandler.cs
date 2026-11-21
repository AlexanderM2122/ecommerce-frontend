
using System.Net;
using System.Net.Http.Headers;
using EcommerceFrontend.Services;

namespace EcommerceFrontend.Middlewares;

public sealed class EnviaBearerDelegatingHandler(
    IHttpContextAccessor httpContextAccessor,
    IAccessTokenStore tokenStore) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var context = httpContextAccessor.HttpContext;

        var sessionId = context?.User
            .FindFirst("session_id")?.Value;

        if (context?.User.Identity?.IsAuthenticated != true ||
            sessionId is null ||
            !tokenStore.TryGet(sessionId, out var jwt))
        {
            return new HttpResponseMessage(
                HttpStatusCode.Unauthorized)
            {
                RequestMessage = request
            };
        }

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", jwt);

        return await base.SendAsync(request, cancellationToken);
    }
}
