using JoinRpg.Dal.CommonEfCore;
using Microsoft.EntityFrameworkCore;

namespace JoinRpg.Dal.JobService;

public class JobScheduleDataDbContext(DbContextOptions<JobScheduleDataDbContext> options) : JoinPostgreSqlEfContextBase(options)
{
    public DbSet<DailyJobRun> DailyJobRuns { get; set; } = null!;
}
