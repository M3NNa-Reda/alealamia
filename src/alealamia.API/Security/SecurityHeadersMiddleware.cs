namespace alealamia.API.Security
{
    /// <summary>
    /// Adds the baseline security headers to every response of the API, including error and 401/403 responses.
    /// The API only returns JSON, so the content security policy is the strictest one; the Swagger UI (Development only)
    /// needs scripts and styles, so it is left without a policy.
    /// </summary>
    public sealed class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task InvokeAsync(HttpContext context)
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                var isSwagger = context.Request.Path.StartsWithSegments("/swagger");

                headers["X-Content-Type-Options"] = "nosniff";
                headers["X-Frame-Options"] = "DENY";
                headers["Referrer-Policy"] = "no-referrer";
                headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

                if (!isSwagger)
                {
                    headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
                }

                // Authenticated API data must not be kept by browsers or shared caches.
                if (context.Request.Path.StartsWithSegments("/api") && !headers.ContainsKey("Cache-Control"))
                {
                    headers["Cache-Control"] = "no-store";
                }

                return Task.CompletedTask;
            });

            return _next(context);
        }
    }
}
