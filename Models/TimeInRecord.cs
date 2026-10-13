namespace GSCFacultyTimeMonitoring.Models;

public class TimeInRecord
{
    public int RecordId { get; set; }
    public int FacultyId { get; set; }
    public Faculty Faculty { get; set; } = null!;

    public DateOnly Date { get; set; }
    public TimeOnly TimeIn { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
