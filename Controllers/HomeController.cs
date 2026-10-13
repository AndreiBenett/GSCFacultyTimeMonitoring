using System.Diagnostics;
using GSCFacultyTimeMonitoring.Data;
using GSCFacultyTimeMonitoring.Models;
using GSCFacultyTimeMonitoring.Services;
using GSCFacultyTimeMonitoring.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GSCFacultyTimeMonitoring.Controllers;

public class HomeController(
    AppDbContext db,
    IPhilippineTimeService clock,
    ICampusNetworkService campusNetwork,
    ILogger<HomeController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var faculty = await db.Faculty
            .AsNoTracking()
            .Where(x => x.Status == "Active")
            .Select(x => new FacultyOptionViewModel
            {
                FacultyId = x.FacultyId,
                FullName = x.FullName,
                Department = x.Department
            })
            .ToListAsync();

        faculty = faculty.OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
        var timedInTodayIds = (await db.TimeInRecords.AsNoTracking()
            .Where(x => x.Date == clock.Today)
            .Select(x => x.FacultyId).ToListAsync()).ToHashSet();
        var currentServerTime = clock.Now;

        return View(new HomeViewModel
        {
            CurrentDate = DateOnly.FromDateTime(currentServerTime.DateTime),
            ServerTimeUnixMilliseconds =
                currentServerTime.ToUnixTimeMilliseconds(),
            Faculty = faculty,
            TimedInTodayIds = timedInTodayIds
        });
    }

    [HttpPost]
    [EnableRateLimiting("timein")]
    public async Task<IActionResult> TimeIn(int facultyId)
    {
        // --------------------------------------------------
        // GSC STAFF WIFI / CAMPUS NETWORK CHECK
        // --------------------------------------------------
        if (!campusNetwork.IsAllowed(HttpContext))
        {
            var blockedIp =
                campusNetwork.GetClientIp(HttpContext);

            logger.LogWarning(
                "Blocked off-campus Time-In attempt from IP {ClientIp}.",
                blockedIp);

            TempData["HomeError"] =
                "Time-In is only allowed while connected to the GSC Staff WiFi.";

            return RedirectToAction(nameof(Index));
        }

        // --------------------------------------------------
        // CHECK SELECTED FACULTY
        // --------------------------------------------------
        var faculty = await db.Faculty
            .FirstOrDefaultAsync(
                x => x.FacultyId == facultyId &&
                     x.Status == "Active");

        if (faculty is null)
        {
            TempData["HomeError"] =
                "Please select a valid active faculty member.";

            return RedirectToAction(nameof(Index));
        }

        // --------------------------------------------------
        // GET CURRENT PHILIPPINE DATE/TIME
        // --------------------------------------------------
        var now = clock.Now;

        var today =
            DateOnly.FromDateTime(now.DateTime);

        // --------------------------------------------------
        // CHECK IF FACULTY ALREADY TIMED IN TODAY
        // --------------------------------------------------
        var existing = await db.TimeInRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.FacultyId == facultyId &&
                     x.Date == today);

        if (existing is not null)
        {
            StoreResult(
                false,
                true,
                FacultyNameFormatter.SurnameFirst(faculty.FullName),
                existing.Date,
                existing.TimeIn,
                "Already recorded. Your attendance today is shown below.");

            return RedirectToAction(nameof(Result));
        }

        // --------------------------------------------------
        // CREATE NEW TIME-IN RECORD
        // --------------------------------------------------
        var record = new TimeInRecord
        {
            FacultyId = facultyId,
            Date = today,
            TimeIn = TimeOnly.FromDateTime(now.DateTime),
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.TimeInRecords.Add(record);

        try
        {
            await db.SaveChangesAsync();

            StoreResult(
                true,
                false,
                FacultyNameFormatter.SurnameFirst(faculty.FullName),
                record.Date,
                record.TimeIn,
                "Time-in recorded successfully! Your attendance has been saved for today.");
        }
        catch (DbUpdateException ex)
            when (IsUniqueConstraintViolation(ex))
        {
            logger.LogInformation(
                ex,
                "Duplicate same-day time-in prevented for faculty {FacultyId}.",
                facultyId);

            var duplicate = await db.TimeInRecords
                .AsNoTracking()
                .FirstAsync(
                    x => x.FacultyId == facultyId &&
                         x.Date == today);

            StoreResult(
                false,
                true,
                FacultyNameFormatter.SurnameFirst(faculty.FullName),
                duplicate.Date,
                duplicate.TimeIn,
                "Already recorded. Your attendance today is shown below.");
        }

        return RedirectToAction(nameof(Result));
    }

    [HttpGet]
    public IActionResult Result()
    {
        if (TempData["TimeInFaculty"] is not string facultyName)
        {
            return RedirectToAction(nameof(Index));
        }

        var dateText =
            TempData["TimeInDate"]?.ToString();

        var timeText =
            TempData["TimeInTime"]?.ToString();

        if (!DateOnly.TryParse(
                dateText,
                out var date) ||
            !TimeOnly.TryParse(
                timeText,
                out var time))
        {
            return RedirectToAction(nameof(Index));
        }

        return View(new TimeInResultViewModel
        {
            IsSuccess = string.Equals(
                TempData["TimeInSuccess"]?.ToString(),
                "True",
                StringComparison.OrdinalIgnoreCase),

            AlreadyRecorded = string.Equals(
                TempData["TimeInAlready"]?.ToString(),
                "True",
                StringComparison.OrdinalIgnoreCase),

            FacultyName = facultyName,
            Date = date,
            Time = time,

            Message =
                TempData["TimeInMessage"]?.ToString()
                ?? string.Empty
        });
    }

    // --------------------------------------------------
    // TEMPORARY PAGE FOR FINDING GSC PUBLIC IP
    // --------------------------------------------------
    [HttpGet]
    public IActionResult NetworkCheck()
    {
        var clientIp =
            campusNetwork.GetClientIp(HttpContext);

        var allowed =
            campusNetwork.IsAllowed(HttpContext);

        return Content(
            $"Detected client IP: {clientIp}\n" +
            $"Campus restriction enabled: " +
            $"{campusNetwork.IsRestrictionEnabled}\n" +
            $"This IP is allowed: {allowed}");
    }

    [HttpGet]
    public IActionResult Error()
        => View(
            new ErrorViewModel
            {
                RequestId =
                    Activity.Current?.Id ??
                    HttpContext.TraceIdentifier
            });

    private void StoreResult(
        bool success,
        bool already,
        string facultyName,
        DateOnly date,
        TimeOnly time,
        string message)
    {
        TempData["TimeInSuccess"] = success;
        TempData["TimeInAlready"] = already;
        TempData["TimeInFaculty"] = facultyName;
        TempData["TimeInDate"] =
            date.ToString("yyyy-MM-dd");

        TempData["TimeInTime"] =
            time.ToString("HH:mm:ss");

        TempData["TimeInMessage"] = message;
    }

    private static bool IsUniqueConstraintViolation(
        DbUpdateException exception)
        => exception.InnerException
               is Npgsql.PostgresException postgresException
           && postgresException.SqlState == "23505";
}