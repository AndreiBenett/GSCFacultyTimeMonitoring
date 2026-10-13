namespace GSCFacultyTimeMonitoring.ViewModels;

public class PeriodAttendanceViewModel
{
    public string Period { get; set; } = "weekly";
    public DateOnly SelectedDate { get; set; }
    public DateOnly Start { get; set; }
    public DateOnly End { get; set; }
    public DateOnly CountedThrough { get; set; }
    public int CompletedWorkingDays { get; set; }
    public TimeOnly LateAfter { get; set; }
    public TimeOnly CloseDayAt { get; set; }
    public string Department { get; set; } = string.Empty;
    public string Search { get; set; } = string.Empty;
    public string Sort { get; set; } = "name_asc";
    public string WorkingDaysLabel { get; set; } = string.Empty;
    public IReadOnlyList<string> Departments { get; set; } = [];
    public IReadOnlyList<PersonnelPeriodRowViewModel> Rows { get; set; } = [];
    public int TotalAbsent => Rows.Sum(x => x.Absences);
    public int TotalLate => Rows.Sum(x => x.Late);
    public int TotalPresent => Rows.Sum(x => x.Present);
}

public class PersonnelPeriodRowViewModel
{
    public int FacultyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public int Present { get; set; }
    public int Absences { get; set; }
    public int Late { get; set; }
    public IReadOnlyList<DateOnly> AbsentDates { get; set; } = [];
    public IReadOnlyList<DateOnly> LateDates { get; set; } = [];
}
