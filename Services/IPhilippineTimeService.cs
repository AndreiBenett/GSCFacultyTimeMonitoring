namespace GSCFacultyTimeMonitoring.Services;

public interface IPhilippineTimeService
{
    DateTimeOffset Now { get; }
    DateOnly Today { get; }
    TimeOnly TimeNow { get; }
}
