namespace Models.Entities;

public class InstructorSubject
{
    public int InstructorId { get; set; }
    public Instructor Instructor { get; set; } = null!;
    public int SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;
}
// 학생 - 과목 매핑
public class StudentSubject
{
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public int SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;
}

// 수업(Session) - 학생 매핑
public class SessionStudent
{
    public int LessonSessionId { get; set; }
    public LessonSession LessonSession { get; set; } = null!;
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
}