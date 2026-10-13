namespace GSCFacultyTimeMonitoring.ViewModels;

public class AdminDashboardViewModel
{
    public DateOnly Today { get; set; }
    public int TimedInToday { get; set; }
    public int TotalActiveFaculty { get; set; }
    public int NotYetTimedIn => Math.Max(0, TotalActiveFaculty - TimedInToday);
    public decimal AttendanceRate => TotalActiveFaculty == 0 ? 0 : Math.Round((decimal)TimedInToday / TotalActiveFaculty * 100, 1);
    public IReadOnlyList<AttendanceRowViewModel> RecentRecords { get; set; } = Array.Empty<AttendanceRowViewModel>();
}

public class AttendanceRowViewModel
{
    public int RecordId { get; set; }
    public int FacultyId { get; set; }
    public string FacultyName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public TimeOnly TimeIn { get; set; }
}
