namespace GSCFacultyTimeMonitoring.ViewModels;

public class HomeViewModel
{
    public DateOnly CurrentDate { get; set; }
    public long ServerTimeUnixMilliseconds { get; set; }
    public IReadOnlyList<FacultyOptionViewModel> Faculty { get; set; } = Array.Empty<FacultyOptionViewModel>();
    public IReadOnlySet<int> TimedInTodayIds { get; set; } = new HashSet<int>();
}

public class FacultyOptionViewModel
{
    public int FacultyId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string DisplayName => GSCFacultyTimeMonitoring.Services.FacultyNameFormatter.SurnameFirst(FullName);
    public string Initial => GSCFacultyTimeMonitoring.Services.FacultyNameFormatter.Initial(FullName);
    public bool IsSeniorHigh => Department.Equals("Senior High School Department", StringComparison.OrdinalIgnoreCase);
}
