namespace Models.Entities;

public class LessonSession
{
    public int Id {get; set;}
    public DateTime Date {get; set;}
    public int TimeSlot {get;set;}
    public int SubjectId { get; set;}
    public Subject Subject {get; set;} = null!;
    public int InstructorId { get; set; }
    public Instructor Instructor { get; set; } = null!;
    
    // 이 수업에 참여하는 학생들 (최대 3명 제한은 서비스 로직에서 검증)
    public ICollection<SessionStudent> SessionStudents { get; set; } = new List<SessionStudent>();

}