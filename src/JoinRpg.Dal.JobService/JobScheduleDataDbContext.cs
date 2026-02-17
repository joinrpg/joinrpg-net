namespace JoinRpg.Dal.JobService;

public class JobScheduleDataDbContext(DbContextOptions<JobScheduleDataDbContext> options) : DbContext(options)
{
    public DbSet<DailyJobRun> DailyJobRuns { get; set; } = null!;
}
