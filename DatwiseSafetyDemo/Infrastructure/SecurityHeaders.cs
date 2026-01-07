using System;
using System.Web;

namespace DatwiseSafetyDemo.Infrastructure
{
    public static class SecurityHeaders
    {
        public static void Apply(HttpResponse response, bool isHttps)
        {
            if (response == null) return;

            // Basic hardening headers (safe defaults for WebForms).
            Set(response, "X-Frame-Options", "DENY");
            Set(response, "X-Content-Type-Options", "nosniff");
            Set(response, "Referrer-Policy", "strict-origin-when-cross-origin");
            Set(response, "Permissions-Policy", "geolocation=(), microphone=(), camera=()");

            // CSP tuned to allow built-in WebForms scripts and Bootstrap.
            // Tighten further in production if you control all inline scripts.
            Set(response, "Content-Security-Policy",
                "default-src 'self'; " +
                "base-uri 'self'; " +
                "object-src 'none'; " +
                "frame-ancestors 'none'; " +
                "form-action 'self'; " +
                "img-src 'self' data:; " +
                "font-src 'self' data:; " +
                "style-src 'self' 'unsafe-inline'; " +
                "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                "connect-src 'self'");

            if (isHttps)
            {
                // Only set HSTS on HTTPS.
                Set(response, "Strict-Transport-Security", "max-age=31536000; includeSubDomains");
            }
        }

        private static void Set(HttpResponse response, string name, string value)
        {
            try
            {
                // Use Headers collection when possible; fall back to AddHeader.
                if (response.Headers != null)
                {
                    response.Headers.Remove(name);
                    response.Headers.Add(name, value);
                }
                else
                {
                    response.AddHeader(name, value);
                }
            }
            catch
            {
                // Ignore header write issues (some hosts lock headers late in pipeline).
            }
        }
    }
}
