namespace GSCFacultyTimeMonitoring.Services;

public interface ICampusNetworkService
{
    bool IsRestrictionEnabled { get; }

    string GetClientIp(HttpContext context);

    bool IsAllowed(HttpContext context);
}