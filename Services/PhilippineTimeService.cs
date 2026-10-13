namespace GSCFacultyTimeMonitoring.Services;

public class PhilippineTimeService : IPhilippineTimeService
{
    private static readonly TimeZoneInfo ManilaTimeZone = ResolveManilaTimeZone();

    public DateTimeOffset Now
    {
        get
        {
            var utc = DateTimeOffset.UtcNow;
            return TimeZoneInfo.ConvertTime(utc, ManilaTimeZone);
        }
    }

    public DateOnly Today => DateOnly.FromDateTime(Now.DateTime);
    public TimeOnly TimeNow => TimeOnly.FromDateTime(Now.DateTime);

    private static TimeZoneInfo ResolveManilaTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
        }
    }
}
