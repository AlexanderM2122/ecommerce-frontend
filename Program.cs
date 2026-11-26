using EcommerceFrontend.Middlewares;
using EcommerceFrontend.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// MVC y protección CSRF global.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

// Almacenamiento del JWT en el servidor.
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IAccessTokenStore, MemoryAccessTokenStore>();

// Autenticación mediante cookies endurecidas.
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth";
        options.AccessDeniedPath = "/Home/AccessDenied";

        options.Cookie.Name = "__Host-EcommerceAuth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.Path = "/";

        options.ExpireTimeSpan = TimeSpan.FromMinutes(19);
        options.SlidingExpiration = false;

        // Comprobar que la sesión exista en el servidor.
        options.Events.OnValidatePrincipal = async context =>
        {
            var sessionId = context.Principal?.FindFirst("session_id")?.Value;
            var store = context.HttpContext.RequestServices.GetRequiredService<IAccessTokenStore>();

            if (sessionId is null || !store.TryGet(sessionId, out _))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });

builder.Services.AddAuthorization();

// Cookie de protección CSRF.
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "__Host-EcommerceXsrf";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.HttpOnly = true;
    options.Cookie.Path = "/";
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<EnviaBearerDelegatingHandler>();

// Configuración centralizada para los clientes hacia Node.js.
void ConfigurarClienteBackend(IServiceProvider services, HttpClient client)
{
    var configuration = services.GetRequiredService<IConfiguration>();
    var environment = services.GetRequiredService<IHostEnvironment>();

    var baseUrl = configuration["BackendApi:BaseUrl"]
        ?? throw new InvalidOperationException("No se configuró la URL del backend.");

    var uri = new Uri(baseUrl, UriKind.Absolute);

    var permiteHttpLocal =
        environment.IsDevelopment() &&
        uri.Scheme == Uri.UriSchemeHttp &&
        uri.IsLoopback;

    if (uri.Scheme != Uri.UriSchemeHttps && !permiteHttpLocal)
        throw new InvalidOperationException("La conexión al backend requiere HTTPS.");

    client.BaseAddress = uri;
    client.Timeout = TimeSpan.FromSeconds(10);
}

// Clientes HTTP hacia Node.js.
builder.Services.AddHttpClient<AuthClientService>(ConfigurarClienteBackend);

builder.Services.AddHttpClient<CategoriasClientService>(ConfigurarClienteBackend)
    .AddHttpMessageHandler<EnviaBearerDelegatingHandler>();

builder.Services.AddHttpClient<ProductosClientService>(ConfigurarClienteBackend)
    .AddHttpMessageHandler<EnviaBearerDelegatingHandler>();

builder.Services.AddHttpClient<UsuariosClientService>(ConfigurarClienteBackend)
    .AddHttpMessageHandler<EnviaBearerDelegatingHandler>();

builder.Services.AddHttpClient<ArchivosClientService>(ConfigurarClienteBackend)
    .AddHttpMessageHandler<EnviaBearerDelegatingHandler>();

builder.Services.AddHttpClient<ComprasClientService>(ConfigurarClienteBackend)
    .AddHttpMessageHandler<EnviaBearerDelegatingHandler>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Cabeceras HTTP defensivas (Protección contra XSS, Clickjacking y MIME-sniffing).
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self' https://cdn.jsdelivr.net; " +
        "style-src 'self' https://cdn.jsdelivr.net; " +
        "img-src 'self' data:; " +
        "font-src 'self' https://cdn.jsdelivr.net; " +
        "object-src 'none'; " +
        "base-uri 'self'; " +
        "frame-ancestors 'none'; " +
        "form-action 'self';";

    await next();
});

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute( 
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();