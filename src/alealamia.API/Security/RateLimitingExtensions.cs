using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace alealamia.API.Security
{
    /// <summary>Limits for the anonymous authentication endpoints (register and login), per client IP address.</summary>
    public sealed class AuthRateLimitOptions
    {
        public const string SectionName = "RateLimiting:Auth";

        public int PermitLimit { get; set; } = 10;

        public int WindowSeconds { get; set; } = 60;
    }

    public static class RateLimitingExtensions
    {
        public const string AuthPolicy = "auth";

        public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
        {
            services.AddOptions<AuthRateLimitOptions>()
                .BindConfiguration(AuthRateLimitOptions.SectionName)
                .Validate(o => o.PermitLimit > 0 && o.WindowSeconds > 0,
                    $"{AuthRateLimitOptions.SectionName}: PermitLimit and WindowSeconds must be greater than zero.")
                .ValidateOnStart();

            services.AddRateLimiter(_ => { });

            // Configured through options so the limits are read when the host is built (and can be overridden per environment).
            services.AddOptions<RateLimiterOptions>()
                .Configure<IOptions<AuthRateLimitOptions>>((limiter, auth) =>
                {
                    var settings = auth.Value;

                    limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                    limiter.AddPolicy(AuthPolicy, context =>
                        RateLimitPartition.GetFixedWindowLimiter(
                            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                            _ => new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = settings.PermitLimit,
                                Window = TimeSpan.FromSeconds(settings.WindowSeconds),
                                QueueLimit = 0
                            }));

                    limiter.OnRejected = async (rejected, cancellationToken) =>
                    {
                        var response = rejected.HttpContext.Response;
                        response.StatusCode = StatusCodes.Status429TooManyRequests;

                        if (rejected.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                        {
                            response.Headers.RetryAfter =
                                ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
                        }

                        await response.WriteAsJsonAsync(new ProblemDetails
                        {
                            Status = StatusCodes.Status429TooManyRequests,
                            Title = "Too many requests",
                            Detail = "Too many attempts. Please wait a moment and try again."
                        }, cancellationToken);
                    };
                });

            return services;
        }
    }
}
