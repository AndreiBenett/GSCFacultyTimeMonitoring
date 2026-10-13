using System.ComponentModel.DataAnnotations;

namespace GSCFacultyTimeMonitoring.Models;

public class Faculty
{
    public int FacultyId { get; set; }

    [Required, StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(100)]
    public string Department { get; set; } = "General";

    [Required, StringLength(20)]
    public string Status { get; set; } = "Active";

    public TimeSpan LateAfter { get; set; } = new TimeSpan(7, 0, 0);

    public ICollection<TimeInRecord> TimeInRecords { get; set; } = new List<TimeInRecord>();
}
