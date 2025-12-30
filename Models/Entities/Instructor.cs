using System.ComponentModel.DataAnnotations;

namespace Models.Entities;
public class Instructor
{
    public int Id { get; set; }
    [Required]
    [MaxLength(20)]
    public string Name { get; set; } = string.Empty;

    public string Education { get; set; } = string.Empty;

    [MaxLength(15)]
    public string PhoneNumber { get; set; } = string.Empty;
    // 선호 시간대를 "1,2,3" 형태의 문자열로 저장 (단순화된 실무 기법)
    public string PreferredTimeSlots { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    // 관계 설정
    public ICollection<InstructorSubject> InstructorSubjects { get; set; } = new List<InstructorSubject>();
    public ICollection<LessonSession> Sessions { get; set; } = new List<LessonSession>();
}