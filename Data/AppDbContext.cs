using Microsoft.EntityFrameworkCore;
using Models.Entities;

namespace Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options){}
    public DbSet<Instructor> Instructors => Set<Instructor>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<LessonSession> LessonSessions => Set<LessonSession>();

    public DbSet<InstructorSubject> InstructorSubjects => Set<InstructorSubject>();
    public DbSet<StudentSubject> StudentSubjects => Set<StudentSubject>();
    public DbSet<SessionStudent> SessionStudents => Set<SessionStudent>();
    

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<InstructorSubject>()
            .HasKey(isub => new {isub.InstructorId, isub.SubjectId});

        // 2. 학생-과목 다대다 (복합키 설정)
        modelBuilder.Entity<StudentSubject>()
            .HasKey(ssub => new { ssub.StudentId, ssub.SubjectId });

        // 3. 수업-학생 다대다 (복합키 설정)
        // 같은 수업에 같은 학생이 중복해서 들어갈 수 없도록 보장합니다.
        modelBuilder.Entity<SessionStudent>()
            .HasKey(sst => new { sst.LessonSessionId, sst.StudentId });

        // 4. 실무적 성능 최적화: 인덱스(Index) 설정
        // 시간표 조회는 '날짜'와 '타임슬롯'으로 자주 검색하므로 인덱스를 겁니다.
        modelBuilder.Entity<LessonSession>()
            .HasIndex(s => new { s.Date, s.TimeSlot });

        // 5. 강사-수업 관계 (1:N)
        modelBuilder.Entity<LessonSession>()
            .HasOne(s => s.Instructor)
            .WithMany(i => i.Sessions)
            .HasForeignKey(s => s.InstructorId)
            .OnDelete(DeleteBehavior.Restrict); 
            // 실무 팁: 강사를 지운다고 수업 기록이 다 사라지면 안 되므로 Restrict(제한) 설정
        
        //학생의 선호 강사 관계
        modelBuilder.Entity<StudentInstructorPreference>()
            .HasKey(p => new { p.StudentId, p.InstructorId });

        // (선택 사항) 관계를 더 명확히 정의하고 싶을 때
        modelBuilder.Entity<StudentInstructorPreference>()
            .HasOne(p => p.Student)
            .WithMany(s => s.InstructorPreferences)
            .HasForeignKey(p => p.StudentId);

        modelBuilder.Entity<StudentInstructorPreference>()
            .HasOne(p => p.Instructor)
            .WithMany() // Instructor 쪽에서는 이 리스트를 굳이 가질 필요가 없다면 비워둡니다.
            .HasForeignKey(p => p.InstructorId);
    }
}