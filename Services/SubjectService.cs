using Microsoft.EntityFrameworkCore;
using Data;
using Models.Entities;

namespace Services;
public class SubjectService
{
    private readonly AppDbContext _context;
    public SubjectService(AppDbContext context) => _context = context;

    public async Task<List<Subject>> GetAllSubjectsAsync() 
        => await _context.Subjects.ToListAsync();
}