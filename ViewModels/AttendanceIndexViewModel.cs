namespace GSCFacultyTimeMonitoring.ViewModels;

public class AttendanceIndexViewModel
{
    public DateOnly Date { get; set; }
    public string Search { get; set; } = string.Empty;
    public string Sort { get; set; } = "name_asc";
    public IReadOnlyList<AttendanceRowViewModel> Records { get; set; } = Array.Empty<AttendanceRowViewModel>();
}
