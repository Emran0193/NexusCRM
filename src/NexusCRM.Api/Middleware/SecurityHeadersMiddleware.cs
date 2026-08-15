namespace NexusCRM.Api.Middleware;

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
            headers["Cross-Origin-Opener-Policy"] = "same-origin";
            headers["Cross-Origin-Resource-Policy"] = "same-site";
            headers.Remove("Server");
            headers.Remove("X-Powered-By");

            // API-centric CSP; SPA is hosted separately on another origin in local dev.
            if (!headers.ContainsKey("Content-Security-Policy"))
            {
                headers["Content-Security-Policy"] =
                    "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'";
            }

            return Task.CompletedTask;
        });

        await _next(context);
    }
}
