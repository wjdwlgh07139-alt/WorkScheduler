using Microsoft.EntityFrameworkCore;
using Data;
using Models.Entities;

namespace Services;

public class InstructorService
{
    private readonly AppDbContext _context;
    public InstructorService(AppDbContext context) => _context = context;

    // 모든 강사 목록 조회 (과목 정보 포함)
    public async Task<List<Instructor>> GetInstructorsAsync()
    {
        return await _context.Instructors
            .Include(i => i.InstructorSubjects)
                .ThenInclude(isub => isub.Subject)
            .ToListAsync();
    }

    // 새 강사 등록 (선택된 과목 ID 리스트 포함)
    public async Task CreateInstructorAsync(Instructor instructor, List<int> selectedSubjectIds)
    {
        Console.WriteLine($"{instructor.Id}, {instructor.Name}");
        _context.Instructors.Add(instructor);
        await _context.SaveChangesAsync(); // 강사 ID 먼저 생성

        // 선택된 과목들을 매핑 테이블에 추가
        foreach (var subjectId in selectedSubjectIds)
        {
            _context.InstructorSubjects.Add(new InstructorSubject
            {
                InstructorId = instructor.Id,
                SubjectId = subjectId
            });
        }
        await _context.SaveChangesAsync();
    }
}