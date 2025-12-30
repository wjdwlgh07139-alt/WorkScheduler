using extensionApp.Components;
using Data;
using Services;
using Models.Entities;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// DB 컨텍스트 등록
// 실무 팁: UseSqlite 내부에 연결 문자열은 보통 appsettings.json에서 가져옵니다.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// 3. 비즈니스 로직 서비스 주입 (실무 표준인 AddScoped 사용)
// Scoped로 등록하면 브라우저를 새로고침하거나 세션이 유지되는 동안 
// 동일한 서비스 인스턴스를 공유합니다.
builder.Services.AddScoped<InstructorService>();
builder.Services.AddScoped<StudentService>();
builder.Services.AddScoped<SubjectService>();  // 과목 목록 조회를 위해 필요
builder.Services.AddScoped<ScheduleService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    // DB가 생성되었는지 확인
    context.Database.EnsureCreated();

    // 과목 데이터가 하나도 없다면 기본 데이터 추가
    if (!context.Subjects.Any())
    {
        context.Subjects.AddRange(
            new Subject { Name = "수학" },
            new Subject { Name = "영어" },
            new Subject { Name = "국어" },
            new Subject { Name = "한국사"},
            new Subject { Name = "통합과학"},
            new Subject { Name = "물리1"},
            new Subject { Name = "생물1"},
            new Subject { Name = "화학1"},
            new Subject { Name = "생명1"},
            new Subject { Name = "물리2"},
            new Subject { Name = "생물2"},
            new Subject { Name = "화학2"},
            new Subject { Name = "생명2"}
        );
        context.SaveChanges();
    }
}

app.Run();
