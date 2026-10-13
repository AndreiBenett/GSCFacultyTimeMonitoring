using System.ComponentModel.DataAnnotations;

namespace GSCFacultyTimeMonitoring.ViewModels;

public class FacultyFormViewModel
{
    public int FacultyId { get; set; }

    [Required, StringLength(150)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Department { get; set; } = "General";

    [Required]
    public string Status { get; set; } = "Active";

    [Display(Name = "Late time")]
    public TimeSpan LateAfter { get; set; } = new TimeSpan(7, 0, 0);
}
