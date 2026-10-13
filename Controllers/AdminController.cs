using ClosedXML.Excel;
using GSCFacultyTimeMonitoring.Data;
using GSCFacultyTimeMonitoring.Services;
using GSCFacultyTimeMonitoring.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GSCFacultyTimeMonitoring.Controllers;

[Authorize(Roles = "Administrator")]
public class AdminController(AppDbContext db, IPhilippineTimeService clock, AttendanceReportService periodReports) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var today = clock.Today;
        var totalActive = await db.Faculty.AsNoTracking().CountAsync(x => x.Status == "Active");
        var timedInToday = await db.TimeInRecords.AsNoTracking().CountAsync(x => x.Date == today && x.Faculty.Status == "Active");

        var recent = await db.TimeInRecords
            .AsNoTracking()
            .Where(x => x.Date == today)
            .OrderByDescending(x => x.TimeIn)
            .Take(10)
            .Select(x => new AttendanceRowViewModel
            {
                RecordId = x.RecordId,
                FacultyId = x.FacultyId,
                FacultyName = x.Faculty.FullName,
                Department = x.Faculty.Department,
                Date = x.Date,
                TimeIn = x.TimeIn
            })
            .ToListAsync();

        foreach (var item in recent) item.FacultyName = FacultyNameFormatter.SurnameFirst(item.FacultyName);

        return View(new AdminDashboardViewModel
        {
            Today = today,
            TimedInToday = timedInToday,
            TotalActiveFaculty = totalActive,
            RecentRecords = recent
        });
    }

    [HttpGet]
    public async Task<IActionResult> Attendance(DateOnly? date, string? search, string sort = "name_asc")
    {
        var selectedDate = date ?? clock.Today;
        var query = db.TimeInRecords.AsNoTracking().Where(x => x.Date == selectedDate);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Faculty.FullName.Contains(term) || x.Faculty.Department.Contains(term));
        }

        var rows = await query.Select(x => new AttendanceRowViewModel
        {
            RecordId = x.RecordId,
            FacultyId = x.FacultyId,
            FacultyName = x.Faculty.FullName,
            Department = x.Faculty.Department,
            Date = x.Date,
            TimeIn = x.TimeIn
        }).ToListAsync();
        foreach (var item in rows) item.FacultyName = FacultyNameFormatter.SurnameFirst(item.FacultyName);
        rows = sort switch
        {
            "name_desc" => rows.OrderByDescending(x => x.FacultyName, StringComparer.OrdinalIgnoreCase).ToList(),
            "time_asc" => rows.OrderBy(x => x.TimeIn).ThenBy(x => x.FacultyName).ToList(),
            "time_desc" => rows.OrderByDescending(x => x.TimeIn).ThenBy(x => x.FacultyName).ToList(),
            _ => rows.OrderBy(x => x.FacultyName, StringComparer.OrdinalIgnoreCase).ToList()
        };

        return View(new AttendanceIndexViewModel
        {
            Date = selectedDate,
            Search = search?.Trim() ?? string.Empty,
            Sort = sort,
            Records = rows
        });
    }

    [HttpGet]
    public async Task<IActionResult> ExportExcel(DateOnly? date, string? search)
    {
        var selectedDate = date ?? clock.Today;
        var query = db.TimeInRecords.AsNoTracking().Where(x => x.Date == selectedDate);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Faculty.FullName.Contains(term) || x.Faculty.Department.Contains(term));
        }

        var records = await query
            .Select(x => new AttendanceRowViewModel
            {
                FacultyName = x.Faculty.FullName,
                Department = x.Faculty.Department,
                Date = x.Date,
                TimeIn = x.TimeIn
            })
            .ToListAsync();

        foreach (var record in records) record.FacultyName = FacultyNameFormatter.SurnameFirst(record.FacultyName);
        records = records.OrderBy(x => x.FacultyName, StringComparer.OrdinalIgnoreCase).ToList();

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Faculty Time-In");

        sheet.Cell("A1").Value = "GOOD SAMARITAN COLLEGES";
        sheet.Cell("A2").Value = "Faculty Time-In Monitoring Report";
        sheet.Cell("A3").Value = selectedDate.ToString("MMMM d, yyyy");
        var titleRange = sheet.Range("A1:D1").Merge();
        titleRange.Style.Font.Bold = true;
        titleRange.Style.Font.FontSize = 16;
        var subtitleRange = sheet.Range("A2:D2").Merge();
        subtitleRange.Style.Font.Bold = true;
        subtitleRange.Style.Font.FontSize = 13;
        sheet.Range("A3:D3").Merge();

        sheet.Cell(5, 1).Value = "Faculty Name";
        sheet.Cell(5, 2).Value = "Department";
        sheet.Cell(5, 3).Value = "Date";
        sheet.Cell(5, 4).Value = "Time In";
        sheet.Range(5, 1, 5, 4).Style.Font.Bold = true;

        var row = 6;
        foreach (var record in records)
        {
            sheet.Cell(row, 1).Value = record.FacultyName;
            sheet.Cell(row, 2).Value = record.Department;
            sheet.Cell(row, 3).Value = record.Date.ToString("yyyy-MM-dd");
            sheet.Cell(row, 4).Value = record.TimeIn.ToString("hh:mm:ss tt");
            row++;
        }

        sheet.Columns().AdjustToContents();
        sheet.SheetView.FreezeRows(5);
        var usedRange = sheet.RangeUsed();
        if (usedRange is not null)
        {
            usedRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            usedRange.Style.Border.InsideBorder = XLBorderStyleValues.Hair;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var fileName = $"GSC-Faculty-TimeIn-{selectedDate:yyyy-MM-dd}.xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet]
    public async Task<IActionResult> PeriodReport(
        string period = "weekly", DateOnly? date = null, string? department = null,
        string? search = null, string sort = "name_asc")
    {
        return View(await periodReports.GetAsync(period, date, department, search, sort));
    }

    [HttpGet]
    public async Task<IActionResult> ExportPeriodExcel(
        string period = "weekly", DateOnly? date = null, string? department = null,
        string? search = null, string sort = "name_asc")
    {
        var report = await periodReports.GetAsync(period, date, department, search, sort);
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(report.Period == "monthly" ? "Monthly Report" : "Weekly Report");
        sheet.Cell(1, 1).Value = "GOOD SAMARITAN COLLEGES";
        sheet.Cell(2, 1).Value = $"{(report.Period == "monthly" ? "Monthly" : "Weekly")} Personnel Attendance Summary";
        sheet.Cell(3, 1).Value = $"{report.Start:MMM d, yyyy} – {report.End:MMM d, yyyy}";
        sheet.Cell(4, 1).Value = $"Working days counted: {report.CompletedWorkingDays} | Late after {report.LateAfter:hh:mm tt} | Day closed at {report.CloseDayAt:hh:mm tt}";
        sheet.Range(1, 1, 1, 7).Merge().Style.Font.Bold = true;
        sheet.Range(2, 1, 2, 7).Merge().Style.Font.Bold = true;
        sheet.Range(3, 1, 3, 7).Merge();
        sheet.Range(4, 1, 4, 7).Merge();
        string[] headers = ["Surname, Given Name", "Department", "Present", "Late", "Absent*", "Late Dates", "Missing Time-In Dates*"];
        for (var c = 0; c < headers.Length; c++) sheet.Cell(6, c + 1).Value = headers[c];
        sheet.Range(6, 1, 6, 7).Style.Font.Bold = true;
        var r = 7;
        foreach (var item in report.Rows)
        {
            sheet.Cell(r, 1).Value = item.Name;
            sheet.Cell(r, 2).Value = item.Department;
            sheet.Cell(r, 3).Value = item.Present;
            sheet.Cell(r, 4).Value = item.Late;
            sheet.Cell(r, 5).Value = item.Absences;
            sheet.Cell(r, 6).Value = string.Join(", ", item.LateDates.Select(x => x.ToString("MMM d")));
            sheet.Cell(r, 7).Value = string.Join(", ", item.AbsentDates.Select(x => x.ToString("MMM d")));
            r++;
        }
        sheet.Cell(r + 1, 1).Value = "* Provisional only: missing time-in is not proof of an unexcused absence.";
        sheet.Range(r + 1, 1, r + 1, 7).Merge();
        sheet.Cell(r + 2, 1).Value = "Review schedules, leaves, holidays, hires and official DTR before using this report for payroll.";
        sheet.Range(r + 2, 1, r + 2, 7).Merge();
        sheet.SheetView.FreezeRows(6);
        sheet.Columns().AdjustToContents();
        sheet.Column(6).Width = Math.Min(50, Math.Max(18, sheet.Column(6).Width));
        sheet.Column(7).Width = Math.Min(60, Math.Max(22, sheet.Column(7).Width));
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var fileName = $"GSC-{report.Period}-personnel-attendance-{report.Start:yyyy-MM-dd}.xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}
