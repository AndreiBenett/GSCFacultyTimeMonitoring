namespace GSCFacultyTimeMonitoring.ViewModels;

public class TimeInResultViewModel
{
    public bool IsSuccess { get; set; }
    public bool AlreadyRecorded { get; set; }
    public string FacultyName { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public TimeOnly Time { get; set; }
    public string Message { get; set; } = string.Empty;
}
