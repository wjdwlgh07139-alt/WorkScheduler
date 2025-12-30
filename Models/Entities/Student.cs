using System.ComponentModel.DataAnnotations;

namespace Models.Entities;
public class Student
{
    public int Id {get; set;}
    [Required]
    [MaxLength(20)]
    public string Name { get; set; } = string.Empty;
    public Grade Grade {get; set;}
    public string PreferredTimeSlots { get; set; } = string.Empty;
    
    public ICollection<StudentSubject> StudentSubjects { get; set; } = new List<StudentSubject>();
    public ICollection<SessionStudent> SessionStudents { get; set; } = new List<SessionStudent>();
    public ICollection<StudentInstructorPreference> InstructorPreferences { get; set; } = new List<StudentInstructorPreference>();
}

public enum Grade
{
    Elementary1 = 1,
    Elementary2,
    Elementary3,
    Elementary4,
    Elementary5,
    Elementary6,
    Middle1,
    Middle2,
    Middle3,
    High1,
    High2,
    High3
}