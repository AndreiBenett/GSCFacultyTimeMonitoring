using System.Net;

namespace GSCFacultyTimeMonitoring.Services;

public sealed class CampusNetworkService(
    IConfiguration configuration,
    IWebHostEnvironment environment) : ICampusNetworkService
{
    public bool IsRestrictionEnabled =>
        configuration.GetValue<bool>("CampusNetwork:Enabled");

    public string GetClientIp(HttpContext context)
    {
        // Render public web services are behind Cloudflare.
        // Cloudflare provides the real client IP using CF-Connecting-IP.
        if (context.Request.Headers.TryGetValue(
                "CF-Connecting-IP",
                out var cfHeader))
        {
            var rawIp = cfHeader.FirstOrDefault();

            if (TryNormalizeIp(rawIp, out var normalizedIp))
            {
                return normalizedIp;
            }
        }

        // For local Visual Studio testing only.
        if (environment.IsDevelopment())
        {
            var remoteIp = context.Connection.RemoteIpAddress;

            if (remoteIp is not null)
            {
                if (remoteIp.IsIPv4MappedToIPv6)
                {
                    remoteIp = remoteIp.MapToIPv4();
                }

                return remoteIp.ToString();
            }
        }

        // In production, if Render/Cloudflare does not provide
        // the trusted IP header, don't guess.
        return "unknown";
    }

    public bool IsAllowed(HttpContext context)
    {
        // Restriction OFF = allow Time-In normally.
        if (!IsRestrictionEnabled)
        {
            return true;
        }

        var clientIp = GetClientIp(context);

        if (!IPAddress.TryParse(clientIp, out var parsedClientIp))
        {
            return false;
        }

        if (parsedClientIp.IsIPv4MappedToIPv6)
        {
            parsedClientIp = parsedClientIp.MapToIPv4();
        }

        var configuredIps =
            configuration["CampusNetwork:AllowedPublicIps"];

        // If restriction is ON but there are no allowed IPs,
        // block the Time-In for safety.
        if (string.IsNullOrWhiteSpace(configuredIps))
        {
            return false;
        }

        foreach (var rawAllowedIp in configuredIps.Split(
                     ',',
                     StringSplitOptions.RemoveEmptyEntries |
                     StringSplitOptions.TrimEntries))
        {
            if (!IPAddress.TryParse(
                    rawAllowedIp,
                    out var allowedIp))
            {
                continue;
            }

            if (allowedIp.IsIPv4MappedToIPv6)
            {
                allowedIp = allowedIp.MapToIPv4();
            }

            if (parsedClientIp.Equals(allowedIp))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryNormalizeIp(
        string? rawIp,
        out string normalizedIp)
    {
        normalizedIp = string.Empty;

        if (!IPAddress.TryParse(
                rawIp?.Trim(),
                out var ip))
        {
            return false;
        }

        if (ip.IsIPv4MappedToIPv6)
        {
            ip = ip.MapToIPv4();
        }

        normalizedIp = ip.ToString();

        return true;
    }
}