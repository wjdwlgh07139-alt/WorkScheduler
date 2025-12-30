using Microsoft.EntityFrameworkCore;
using Data;
using Models.Entities;

namespace Services;

public class StudentService
{
    private readonly AppDbContext _context;
    public StudentService(AppDbContext context) => _context = context;

    public async Task<List<Student>> GetStudentsAsync()
    {
        return await _context.Students
            .Include(s => s.StudentSubjects)
                .ThenInclude(ss => ss.Subject)
            .ToListAsync();
    }

    public async Task CreateStudentAsync(Student student, List<int> selectedSubjectIds)
    {
        _context.Students.Add(student);
        await _context.SaveChangesAsync();

        foreach (var subjectId in selectedSubjectIds)
        {
            _context.StudentSubjects.Add(new StudentSubject
            {
                StudentId = student.Id,
                SubjectId = subjectId
            });
        }
        await _context.SaveChangesAsync();
    }
}