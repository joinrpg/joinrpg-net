using System;
using System.Data.Entity;
using System.Threading.Tasks;
using JoinRpg.Dal.Impl.Repositories;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;

namespace JoinRpg.Dal.Impl
{
  public class MyDbContext : DbContext, IUnitOfWork
  {
    public MyDbContext() : base("DefaultConnection")
    { }

    public DbSet<Project> ProjectsSet => Set<Project>();

    public DbSet<User> UserSet => Set<User>();

    public DbSet<Claim> ClaimSet => Set<Claim>();

    DbSet<T> IUnitOfWork.GetDbSet<T>() => Set<T>();

    Task IUnitOfWork.SaveChangesAsync() => SaveChangesAsync();

    public IUserRepository GetUsersRepository() => new UserInfoRepository(this);
    public IProjectRepository GetProjectRepository()
    {
      return new ProjectRepository(this);
    }

    public IClaimsRepository GetClaimsRepository() => new ClaimsRepositoryImpl(this);

    public static MyDbContext Create()
    {
      return new MyDbContext();
    }

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
      modelBuilder.Entity<ProjectAcl>().HasKey(c => new {c.UserId, c.ProjectId});

      modelBuilder.Entity<Project>().HasRequired(p => p.Details).WithRequiredPrincipal();
      modelBuilder.Entity<ProjectDetails>().HasKey(pd => pd.ProjectId);
      modelBuilder.Entity<ProjectAcl>().HasKey(acl => acl.ProjectAclId);
      
      modelBuilder.Entity<CharacterGroup>().HasMany(cg => cg.ParentGroups).WithMany(cg => cg.ChildGroups);
      modelBuilder.Entity<CharacterGroup>()
        .HasOptional(c => c.ResponsibleMasterUser)
        .WithMany()
        .HasForeignKey(c => c.ResponsibleMasterUserId);

      modelBuilder.Entity<Character>().HasMany(c => c.Groups).WithMany(cg => cg.Characters);

      modelBuilder.Entity<Project>().HasMany(p => p.Characters).WithRequired(c => c.Project).WillCascadeOnDelete(false);

      modelBuilder.Entity<CharacterGroup>().HasMany(cg =>cg.Claims).WithOptional(c => c.Group).WillCascadeOnDelete(false);
      modelBuilder.Entity<Character>().HasMany(cg => cg.Claims).WithOptional(c => c.Character).WillCascadeOnDelete(false);
      modelBuilder.Entity<Claim>().HasRequired(c => c.Player). WithMany(p => p.Claims).WillCascadeOnDelete(false);
      modelBuilder.Entity<Claim>().HasRequired(c => c.Project).WithMany(p => p.Claims).WillCascadeOnDelete(false);

      modelBuilder.Entity<Claim>().HasMany(c => c.Comments).WithRequired(c => c.Claim);

      modelBuilder.Entity<Claim>()
        .HasOptional(c => c.ResponsibleMasterUser)
        .WithMany()
        .HasForeignKey(c => c.ResponsibleMasterUserId);


      modelBuilder.Entity<Comment>().HasMany(c => c.ChildsComments).WithOptional(comment => comment.Parent).WillCascadeOnDelete(false);
      modelBuilder.Entity<Comment>().HasRequired(comment => comment.Project).WithMany().WillCascadeOnDelete(false);
      modelBuilder.Entity<Comment>().HasRequired(comment => comment.Author).WithMany().WillCascadeOnDelete(false);
      modelBuilder.Entity<Comment>().HasRequired(c => c.Finance).WithRequiredPrincipal(fo => fo.Comment);

      modelBuilder.Entity<FinanceOperation>().HasKey(fo => fo.CommentId);

      modelBuilder.Entity<PlotFolder>().HasMany(pf => pf.RelatedGroups).WithMany(cg => cg.DirectlyRelatedPlotFolders);
      modelBuilder.Entity<PlotFolder>().HasRequired(pf => pf.Project).WithMany(p => p.PlotFolders).WillCascadeOnDelete(false);

      modelBuilder.Entity<PlotElement>().HasMany(pe => pe.TargetCharacters).WithMany(c => c.DirectlyRelatedPlotElements);
      modelBuilder.Entity<PlotElement>().HasMany(pe => pe.TargetGroups).WithMany(c => c.DirectlyRelatedPlotElements);
      modelBuilder.Entity<PlotElement>().HasRequired(pf => pf.Project).WithMany().WillCascadeOnDelete(false);

      modelBuilder.Entity<User>().HasRequired(u => u.Auth).WithRequiredPrincipal();
      modelBuilder.Entity<UserAuthDetails>().HasKey(uad => uad.UserId);

      modelBuilder.Entity<User>().HasRequired(u => u.Allrpg).WithRequiredPrincipal();
      modelBuilder.Entity<AllrpgUserDetails>().HasKey(a => a.UserId);

      modelBuilder.Entity<User>().HasRequired(u => u.Extra).WithRequiredPrincipal();
      modelBuilder.Entity<UserExtra>().HasKey(a => a.UserId);
      
      modelBuilder.Entity<User>().HasMany(u => u.Subscriptions).WithRequired(s => s.User).WillCascadeOnDelete(true);
      modelBuilder.Entity<UserSubscription>().HasRequired(us => us.Project).WithMany().WillCascadeOnDelete(false);
      modelBuilder.Entity<UserSubscription>()
        .HasOptional(us => us.Claim)
        .WithMany(c => c.Subscriptions)
        .HasForeignKey(us => us.ClaimId)
        .WillCascadeOnDelete(false);

      modelBuilder.Entity<FinanceOperation>()
        .HasRequired(finance => finance.MasterUser)
        .WithMany()
        .HasForeignKey(finance => finance.MasterUserId);
      base.OnModelCreating(modelBuilder);
    }
 }
}
