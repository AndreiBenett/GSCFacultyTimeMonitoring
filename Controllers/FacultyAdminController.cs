using GSCFacultyTimeMonitoring.Data;
using GSCFacultyTimeMonitoring.Models;
using GSCFacultyTimeMonitoring.Services;
using GSCFacultyTimeMonitoring.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GSCFacultyTimeMonitoring.Controllers;

[Authorize(Roles = "Administrator")]
public class FacultyAdminController(AppDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? search)
    {
        var query = db.Faculty.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.FullName.Contains(term) || x.Department.Contains(term));
        }

        return View(new FacultyIndexViewModel
        {
            Search = search?.Trim() ?? string.Empty,
            Faculty = (await query.ToListAsync()).OrderBy(x => FacultyNameFormatter.SurnameFirst(x.FullName), StringComparer.OrdinalIgnoreCase).ToList()
        });
    }

    [HttpGet]
    public IActionResult Create() => View("Form", new FacultyFormViewModel());

    [HttpPost]
    public async Task<IActionResult> Create(FacultyFormViewModel model)
    {
        Normalize(model);
        ValidateNormalized(model);
        var existingNames = await db.Faculty.AsNoTracking().Select(x => x.FullName).ToListAsync();
        if (existingNames.Any(name => string.Equals(
            FacultyNameFormatter.SurnameFirst(name), FacultyNameFormatter.SurnameFirst(model.FullName),
            StringComparison.OrdinalIgnoreCase)))
        {
            ModelState.AddModelError(nameof(model.FullName), "A personnel member with this surname and given name already exists.");
        }

        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        db.Faculty.Add(new Faculty
        {
            FullName = model.FullName,
            Department = model.Department,
            Status = model.Status,
            LateAfter = model.LateAfter
        });
        await db.SaveChangesAsync();
        TempData["AdminSuccess"] = "Faculty member added successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var faculty = await db.Faculty.AsNoTracking().FirstOrDefaultAsync(x => x.FacultyId == id);
        if (faculty is null) return NotFound();

        return View("Form", new FacultyFormViewModel
        {
            FacultyId = faculty.FacultyId,
            FullName = faculty.FullName,
            Department = faculty.Department,
            Status = faculty.Status,
            LateAfter = faculty.LateAfter
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(FacultyFormViewModel model)
    {
        Normalize(model);
        ValidateNormalized(model);
        var otherNames = await db.Faculty.AsNoTracking()
            .Where(x => x.FacultyId != model.FacultyId).Select(x => x.FullName).ToListAsync();
        if (otherNames.Any(name => string.Equals(
            FacultyNameFormatter.SurnameFirst(name), FacultyNameFormatter.SurnameFirst(model.FullName),
            StringComparison.OrdinalIgnoreCase)))
        {
            ModelState.AddModelError(nameof(model.FullName), "A personnel member with this surname and given name already exists.");
        }

        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        var faculty = await db.Faculty.FindAsync(model.FacultyId);
        if (faculty is null) return NotFound();

        faculty.FullName = model.FullName;
        faculty.Department = model.Department;
        faculty.Status = model.Status;
        faculty.LateAfter = model.LateAfter;
        await db.SaveChangesAsync();

        TempData["AdminSuccess"] = "Faculty details updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var faculty = await db.Faculty.FindAsync(id);
        if (faculty is null) return NotFound();

        faculty.Status = faculty.Status == "Active" ? "Inactive" : "Active";
        await db.SaveChangesAsync();
        TempData["AdminSuccess"] = $"{faculty.FullName} is now {faculty.Status.ToLowerInvariant()}.";
        return RedirectToAction(nameof(Index));
    }

    private void ValidateNormalized(FacultyFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.FullName))
        {
            ModelState.AddModelError(nameof(model.FullName), "Full name is required.");
        }
        if (string.IsNullOrWhiteSpace(model.Department))
        {
            ModelState.AddModelError(nameof(model.Department), "Department is required.");
        }
    }

    private static void Normalize(FacultyFormViewModel model)
    {
        model.FullName = model.FullName?.Trim() ?? string.Empty;
        model.Department = model.Department?.Trim() ?? string.Empty;
        model.Status = model.Status is "Inactive" ? "Inactive" : "Active";
    }
}
