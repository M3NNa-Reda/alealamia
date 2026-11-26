using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;

namespace alealamia.API.Security
{
    /// <summary>
    /// Browser origins allowed to call the API. Empty (the default) means no cross-origin access at all,
    /// so production must list the real front-end origin explicitly, for example "https://app.example.com".
    /// </summary>
    public sealed class FrontendCorsOptions
    {
        public const string SectionName = "Cors";

        public string[] AllowedOrigins { get; set; } = Array.Empty<string>();

        public IEnumerable<string> NormalizedOrigins =>
            AllowedOrigins.Select(o => o.Trim().TrimEnd('/'));

        public static bool IsValidOrigin(string origin)
        {
            return Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                && uri.AbsolutePath == "/"
                && string.IsNullOrEmpty(uri.Query)
                && string.IsNullOrEmpty(uri.Fragment);
        }
    }

    public static class CorsExtensions
    {
        public const string FrontendPolicy = "frontend";

        public static IServiceCollection AddFrontendCors(this IServiceCollection services)
        {
            services.AddOptions<FrontendCorsOptions>()
                .BindConfiguration(FrontendCorsOptions.SectionName)
                .Validate(
                    o => o.NormalizedOrigins.All(FrontendCorsOptions.IsValidOrigin),
                    "Cors:AllowedOrigins must contain absolute http(s) origins without a path, for example https://app.example.com. Wildcards are not allowed.")
                .ValidateOnStart();

            services.AddCors();

            services.AddOptions<CorsOptions>()
                .Configure<IOptions<FrontendCorsOptions>>((cors, frontend) =>
                {
                    var origins = frontend.Value.NormalizedOrigins.Distinct().ToArray();

                    cors.AddPolicy(FrontendPolicy, policy =>
                    {
                        if (origins.Length == 0)
                            return; // no origin is allowed

                        // Authentication uses a bearer token, not cookies, so credentials are not enabled.
                        policy.WithOrigins(origins)
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .WithExposedHeaders("Retry-After")
                            .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
                    });
                });

            return services;
        }
    }
}
