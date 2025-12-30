namespace Models.Entities;

public enum PreferenceType
{
    Preferred = 1,    // 선호
    Avoid = -1,       // 기피 (비선호)
    Neutral = 0       // 중립
}

public class StudentInstructorPreference
{
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public int InstructorId { get; set; }
    public Instructor Instructor { get; set; } = null!;

    // 선호인지 비선호인지 구분
    public PreferenceType Type { get; set; }
}