using Microsoft.EntityFrameworkCore;
using Data;
using Models.Entities;

namespace Services;

public class ScheduleService
{
    private readonly AppDbContext _context;

    public ScheduleService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 새로운 수업 세션을 생성하고 학생들을 배정합니다.
    /// </summary>
    public async Task<(bool Success, string Message)> CreateSessionAsync(DateTime date, int timeSlot, int instructorId, int subjectId, List<int> studentIds)
    {
        // 실무 기법: 트랜잭션 시작 (여러 테이블 작업을 하나로 묶어 데이터 무결성 보장)
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // 1. 강사 유효성 및 중복 배정 검사
            var instructor = await _context.Instructors
                .FirstOrDefaultAsync(i => i.Id == instructorId);

            if (instructor == null) return (false, "존재하지 않는 강사입니다.");

            // 규칙: 강사의 선호 시간대 확인
            if (!instructor.PreferredTimeSlots.Split(',').Contains(timeSlot.ToString()))
                return (false, "강사의 선호 시간대가 아닙니다.");

            // 규칙: 해당 타임에 강사가 이미 다른 수업이 있는지 확인
            bool isInstructorBusy = await _context.LessonSessions
                .AnyAsync(s => s.Date.Date == date.Date && s.TimeSlot == timeSlot && s.InstructorId == instructorId);
            if (isInstructorBusy) return (false, "해당 강사는 이미 해당 타임에 수업이 배정되어 있습니다.");

            // 2. 학생 수 제한 (최대 3명) 및 중복 검사
            if (studentIds.Count > 3) return (false, "한 수업에 학생은 최대 3명까지만 배정 가능합니다.");

            // 3. 수업 세션 생성
            var newSession = new LessonSession
            {
                Date = date.Date,
                TimeSlot = timeSlot,
                InstructorId = instructorId,
                SubjectId = subjectId
            };

            _context.LessonSessions.Add(newSession);
            await _context.SaveChangesAsync(); // ID 생성을 위해 먼저 저장

            // 4. 학생 매핑 데이터 생성 및 검증
            foreach (var studentId in studentIds)
            {
                // 규칙: 학생이 해당 타임에 다른 수업에 참여 중인지 확인
                bool isStudentBusy = await _context.SessionStudents
                    .Include(ss => ss.LessonSession)
                    .AnyAsync(ss => ss.LessonSession.Date.Date == date.Date && 
                                    ss.LessonSession.TimeSlot == timeSlot && 
                                    ss.StudentId == studentId);
                
                if (isStudentBusy)
                {
                    await transaction.RollbackAsync(); // 작업 전체 취소
                    return (false, $"학생 ID {studentId}는 해당 타임에 이미 다른 수업이 있습니다.");
                }

                _context.SessionStudents.Add(new SessionStudent
                {
                    LessonSessionId = newSession.Id,
                    StudentId = studentId
                });
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync(); // 모든 작업 확정

            return (true, "수업 배정이 성공적으로 완료되었습니다.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            // 실무에서는 여기에 로깅(Logging)을 추가합니다.
            return (false, $"오류 발생: {ex.Message}");
        }
    }

    /// <summary>
    /// 특정 날짜의 전체 시간표 조회 (화면 출력용)
    /// </summary>
    public async Task<List<LessonSession>> GetScheduleByDateAsync(DateTime date)
    {
        return await _context.LessonSessions
            .Include(s => s.Instructor)
            .Include(s => s.Subject)
            .Include(s => s.SessionStudents)
                .ThenInclude(ss => ss.Student)
            .Where(s => s.Date.Date == date.Date)
            .OrderBy(s => s.TimeSlot)
            .ToListAsync();
    }
    public async Task<(int AssignedCount, string Message)> AutoAssignAsync(DateTime date)
    {
        // 1. 기초 데이터 로드 (과목 정보 포함)
        var instructors = await _context.Instructors
            .Include(i => i.InstructorSubjects)
            .ToListAsync();

        var students = await _context.Students
            .Include(s => s.StudentSubjects)
            .Include(s => s.InstructorPreferences) // 선호도 포함
            .ToListAsync();

        // 이미 배정된 학생/강사 제외를 위해 현재 날짜의 세션 로드
        var existingSessions = await _context.LessonSessions
            .Include(s => s.SessionStudents)
            .Where(s => s.Date.Date == date.Date)
            .ToListAsync();

        int assignedCount = 0;
        var isWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
        int maxSlot = isWeekend ? 4 : 3;

        // 2. 타임슬롯별로 루프를 돌며 배정 시도
        for (int slot = 1; slot <= maxSlot; slot++)
        {
            foreach (var instructor in instructors)
            {
                // 규칙 1: 강사가 이 타임에 이미 수업이 있는지 확인
                if (existingSessions.Any(s => s.TimeSlot == slot && s.InstructorId == instructor.Id))
                    continue;

                // 규칙 2: 강사의 선호 시간대인지 확인
                if (!instructor.PreferredTimeSlots.Split(',').Contains(slot.ToString()))
                    continue;

                // 규칙 3: 배정되지 않은 학생 중 강사와 과목이 맞고, 선호 시간이 맞는 학생 최대 3명 찾기
                var availableStudents = students
                    .Where(std => !IsStudentAssigned(existingSessions, std.Id, slot))
                    .Where(std => std.PreferredTimeSlots.Split(',').Contains(slot.ToString()))
                    .Where(std => HasMatchingSubject(instructor, std, out _))
                    // 규칙 4: 기피(Avoid) 대상으로 등록된 강사는 제외 (절대 규칙)
                    .Where(std => !std.InstructorPreferences.Any(p => 
                        p.InstructorId == instructor.Id && p.Type == PreferenceType.Avoid))
                    // 규칙 5: 선호(Preferred)하는 강사를 목록 상단으로 올림 (우선순위)
                    .OrderByDescending(std => std.InstructorPreferences.Any(p => 
                        p.InstructorId == instructor.Id && p.Type == PreferenceType.Preferred))
                    .Take(3)
                    .ToList();

                if (availableStudents.Any())
                {
                    // 3. 수업(Session) 생성
                    var matchedSubjectId = GetCommonSubjectId(instructor, availableStudents[0]);
                    
                    var session = new LessonSession
                    {
                        Date = date.Date,
                        TimeSlot = slot,
                        InstructorId = instructor.Id,
                        SubjectId = matchedSubjectId
                    };

                    _context.LessonSessions.Add(session);
                    await _context.SaveChangesAsync(); // 세션 ID 생성

                    foreach (var std in availableStudents)
                    {
                        _context.SessionStudents.Add(new SessionStudent 
                        { 
                            LessonSessionId = session.Id, 
                            StudentId = std.Id 
                        });
                    }
                    
                    assignedCount++;
                    // 새로 생성된 세션을 기존 세션 리스트에 추가 (다음 루프에서 중복 방지)
                    existingSessions.Add(session); 
                }
            }
        }

        await _context.SaveChangesAsync();
        return (assignedCount, $"{assignedCount}개의 수업이 자동 배정되었습니다.");
    }

    // 헬퍼 메서드: 학생이 해당 타임에 이미 수업이 있는지 체크
    private bool IsStudentAssigned(List<LessonSession> sessions, int studentId, int slot)
    {
        return sessions.Any(s => s.TimeSlot == slot && s.SessionStudents.Any(ss => ss.StudentId == studentId));
    }

    // 헬퍼 메서드: 강사와 학생의 공통 과목 찾기
    private bool HasMatchingSubject(Instructor inst, Student std, out int subjectId)
    {
        var common = inst.InstructorSubjects.Select(isub => isub.SubjectId)
                    .Intersect(std.StudentSubjects.Select(ssub => ssub.SubjectId))
                    .FirstOrDefault();
        
        subjectId = common;
        return common != 0;
    }
    private int GetCommonSubjectId(Instructor inst, Student std)
    {
        // 강사의 과목 ID 리스트와 학생의 과목 ID 리스트에서 교집합을 찾습니다.
        var commonSubjectId = inst.InstructorSubjects
            .Select(isub => isub.SubjectId)
            .Intersect(std.StudentSubjects.Select(ssub => ssub.SubjectId))
            .FirstOrDefault();

        // 공통 과목이 없다면 0을 반환하거나, 비즈니스 규칙에 따라 예외를 던질 수 있습니다.
        return commonSubjectId;
    }
}