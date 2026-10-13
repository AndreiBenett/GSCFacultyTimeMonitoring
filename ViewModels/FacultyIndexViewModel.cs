using GSCFacultyTimeMonitoring.Models;

namespace GSCFacultyTimeMonitoring.ViewModels;

public class FacultyIndexViewModel
{
    public string Search { get; set; } = string.Empty;
    public IReadOnlyList<Faculty> Faculty { get; set; } = Array.Empty<Faculty>();
}
