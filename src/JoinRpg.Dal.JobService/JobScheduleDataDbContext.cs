using EntityFramework.Exceptions.PostgreSQL;
using Microsoft.EntityFrameworkCore;

namespace JoinRpg.Dal.JobService;

public class JobScheduleDataDbContext(DbContextOptions<JobScheduleDataDbContext> options) : DbContext(options)
{
    public DbSet<DailyJobRun> DailyJobRuns { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseExceptionProcessor(); // sane index exceptions
        base.OnConfiguring(optionsBuilder);
    }
}
