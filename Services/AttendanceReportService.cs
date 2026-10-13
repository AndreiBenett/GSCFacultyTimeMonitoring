using System.Globalization;
using GSCFacultyTimeMonitoring.Data;
using GSCFacultyTimeMonitoring.Services;
using GSCFacultyTimeMonitoring.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace GSCFacultyTimeMonitoring.Services;

public class AttendanceRules
{
    // Samples only: the administrator must set these to the school's real policy.
    public string LateAfter { get; set; } = "07:00";
    public string CloseDayAt { get; set; } = "17:00";
    public int[] WorkingDays { get; set; } = [1, 2, 3, 4, 5]; // 0=Sun, 1=Mon...6=Sat
    public string[] NonWorkingDates { get; set; } = []; // yyyy-MM-dd holidays / closures
}

/// <summary>
/// Reports missing time-ins as PROVISIONAL absences only. This system has no leave,
/// personnel start-date, or individual-shift records, so never present them as payroll DTR.
/// The current unfinished workday is excluded from all summary counts.
/// </summary>
public class AttendanceReportService(AppDbContext db, IPhilippineTimeService clock, IConfiguration config)
{
    public async Task<PeriodAttendanceViewModel> GetAsync(
        string period, DateOnly? date, string? department, string? search, string? sort)
    {
        var selected = date ?? clock.Today;
        if (selected.Year < 2000 || selected.Year > 2100)
            selected = clock.Today;
        period = period == "monthly" ? "monthly" : "weekly";
        sort = sort is "late_desc" or "late_asc" or "absent_desc" or "absent_asc"
            or "name_desc" ? sort : "name_asc";

        var start = period == "monthly"
            ? new DateOnly(selected.Year, selected.Month, 1)
            : selected.AddDays(-(((int)selected.DayOfWeek + 6) % 7));
        var end = period == "monthly" ? start.AddMonths(1).AddDays(-1) : start.AddDays(6);
        var rules = config.GetSection("AttendanceRules").Get<AttendanceRules>() ?? new AttendanceRules();
        if (!TimeOnly.TryParseExact(rules.LateAfter, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var lateAfter))
            lateAfter = new TimeOnly(8, 0);
        if (!TimeOnly.TryParseExact(rules.CloseDayAt, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var closeDayAt))
            closeDayAt = new TimeOnly(17, 0);

        var excludedDates = new HashSet<DateOnly>();
        foreach (var value in rules.NonWorkingDates ?? [])
            if (DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var excluded))
                excludedDates.Add(excluded);
        var workdays = (rules.WorkingDays ?? []).ToHashSet();
        var currentDay = clock.Today;
        var currentTime = TimeOnly.FromDateTime(clock.Now.DateTime);
        var lastClosedDay = currentTime >= closeDayAt ? currentDay : currentDay.AddDays(-1);
        var countedDays = new List<DateOnly>();
        for (var d = start; d <= end && d <= lastClosedDay; d = d.AddDays(1))
            if (workdays.Contains((int)d.DayOfWeek) && !excludedDates.Contains(d))
                countedDays.Add(d);

        var staff = await db.Faculty.AsNoTracking()
            .Where(x => x.Status == "Active")
            .Select(x => new { x.FacultyId, x.FullName, x.Department })
            .ToListAsync();
        var departments = staff.Select(x => x.Department).Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
        if (!string.IsNullOrWhiteSpace(department))
            staff = staff.Where(x => string.Equals(x.Department, department, StringComparison.OrdinalIgnoreCase)).ToList();
        if (!string.IsNullOrWhiteSpace(search))
            staff = staff.Where(x => x.FullName.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase)
                || FacultyNameFormatter.SurnameFirst(x.FullName).Contains(search.Trim(), StringComparison.OrdinalIgnoreCase)
                || x.Department.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();

        // Read only the needed date range and the selected staff; no migration is needed.
        var ids = staff.Select(x => x.FacultyId).ToArray();
        var attendance = await db.TimeInRecords.AsNoTracking()
            .Where(x => ids.Contains(x.FacultyId) && x.Date >= start && x.Date <= end && x.Date <= lastClosedDay)
            .Select(x => new { x.FacultyId, x.Date, x.TimeIn })
            .ToListAsync();
        var byPerson = attendance.GroupBy(x => x.FacultyId).ToDictionary(g => g.Key,
            g => g.ToDictionary(x => x.Date, x => x.TimeIn));

        var rows = staff.Select(person =>
        {
            var times = byPerson.GetValueOrDefault(person.FacultyId) ?? new Dictionary<DateOnly, TimeOnly>();
            var absent = countedDays.Where(d => !times.ContainsKey(d)).ToArray();
            var attended = countedDays.Where(times.ContainsKey).ToArray();
            var late = attended.Where(d => times[d] > lateAfter).ToArray();
            return new PersonnelPeriodRowViewModel
            {
                FacultyId = person.FacultyId,
                Name = FacultyNameFormatter.SurnameFirst(person.FullName),
                Department = person.Department,
                Present = attended.Length,
                Absences = absent.Length,
                Late = late.Length,
                AbsentDates = absent,
                LateDates = late
            };
        });
        rows = sort switch
        {
            "name_desc" => rows.OrderByDescending(x => x.Name, StringComparer.OrdinalIgnoreCase),
            "late_desc" => rows.OrderByDescending(x => x.Late).ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase),
            "late_asc" => rows.OrderBy(x => x.Late).ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase),
            "absent_desc" => rows.OrderByDescending(x => x.Absences).ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase),
            "absent_asc" => rows.OrderBy(x => x.Absences).ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase),
            _ => rows.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
        };

        return new PeriodAttendanceViewModel
        {
            Period = period,
            SelectedDate = selected,
            Start = start,
            End = end,
            CountedThrough = lastClosedDay < end ? lastClosedDay : end,
            CompletedWorkingDays = countedDays.Count,
            LateAfter = lateAfter,
            CloseDayAt = closeDayAt,
            Department = department ?? string.Empty,
            Search = search?.Trim() ?? string.Empty,
            Sort = sort,
            Departments = departments,
            Rows = rows.ToArray(),
            WorkingDaysLabel = string.Join(", ", workdays.OrderBy(x => x).Select(x => ((DayOfWeek)x).ToString()))
        };
    }
}
