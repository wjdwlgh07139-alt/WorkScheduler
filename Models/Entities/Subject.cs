using System.ComponentModel.DataAnnotations;

namespace Models.Entities;

public class Subject
{
    public int Id {get; set;}
    [Required]
    [MaxLength(50)]
    public string Name {get; set;} = string.Empty;
    public ICollection<InstructorSubject> InstructorSubjects { get; set; } = new List<InstructorSubject>();
    public ICollection<StudentSubject> StudentSubjects { get; set; } = new List<StudentSubject>();
}