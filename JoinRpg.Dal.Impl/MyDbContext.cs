using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Dal.Impl.Repositories;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;

namespace JoinRpg.Dal.Impl
{
  [UsedImplicitly]
  public class MyDbContext : DbContext, IUnitOfWork
  {
    public MyDbContext() : base("DefaultConnection")
    {
      Database.Log = sql =>
      {
        System.Diagnostics.Debug.WriteLine(sql);
      };
    }

    public DbSet<Project> ProjectsSet => Set<Project>();

    public DbSet<User> UserSet => Set<User>();

    public DbSet<Claim> ClaimSet => Set<Claim>();

    public DbSet<GameReport2DTemplate> GameReport2DTemplates { get; set; }

    DbSet<T> IUnitOfWork.GetDbSet<T>() => Set<T>();

    Task IUnitOfWork.SaveChangesAsync() => SaveChangesAsync();

    public IUserRepository GetUsersRepository() => new UserInfoRepository(this);
    public IProjectRepository GetProjectRepository() => new ProjectRepository(this);

    public IClaimsRepository GetClaimsRepository() => new ClaimsRepositoryImpl(this);
    public IPlotRepository GetPlotRepository() => new PlotRepositoryImpl(this);
    public IForumRepository GetForumRepository() => new ForumRepositoryImpl(this);
    public ICharacterRepository GetCharactersRepository() => new CharacterRepositoryImpl(this);

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
      modelBuilder.Entity<ProjectAcl>().HasKey(c => new {c.UserId, c.ProjectId});
      modelBuilder.Entity<ProjectAcl>()
        .Property(acl => acl.Token)
        .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Computed);

      modelBuilder.Entity<Project>().HasRequired(p => p.Details).WithRequiredPrincipal();
      modelBuilder.Entity<ProjectDetails>().HasKey(pd => pd.ProjectId);
      modelBuilder.Entity<ProjectAcl>().HasKey(acl => acl.ProjectAclId);
      
      
      modelBuilder.Entity<CharacterGroup>()
        .HasOptional(c => c.ResponsibleMasterUser)
        .WithMany()
        .HasForeignKey(c => c.ResponsibleMasterUserId);


      modelBuilder.Entity<Project>().HasMany(p => p.Characters).WithRequired(c => c.Project).WillCascadeOnDelete(false);

      modelBuilder.Entity<Claim>().HasOptional(c => c.Group).WithMany().WillCascadeOnDelete(false);
      modelBuilder.Entity<Claim>().HasOptional(c => c.Character).WithMany().HasForeignKey(c => c.CharacterId).WillCascadeOnDelete(false);
      modelBuilder.Entity<Claim>().HasRequired(c => c.Player).WithMany(p => p.Claims).WillCascadeOnDelete(false);
      modelBuilder.Entity<Claim>().HasRequired(c => c.Project).WithMany(p => p.Claims).WillCascadeOnDelete(false);

      modelBuilder.Entity<Character>().HasOptional(c => c.ApprovedClaim).WithMany()
        .HasForeignKey(c => c.ApprovedClaimId).WillCascadeOnDelete(false);

      modelBuilder.Entity<CommentDiscussion>().HasMany(c => c.Comments).WithRequired(c => c.Discussion);

      modelBuilder.Entity<Claim>()
        .HasRequired(c => c.CommentDiscussion)
        .WithMany()
        .HasForeignKey(c => c.CommentDiscussionId)
        .WillCascadeOnDelete(false);
      modelBuilder.Entity<ForumThread>()
        .HasRequired(c => c.CommentDiscussion)
        .WithMany()
        .HasForeignKey(ft => ft.CommentDiscussionId)
        .WillCascadeOnDelete(false);

      modelBuilder.Entity<Claim>()
        .HasOptional(c => c.ResponsibleMasterUser)
        .WithMany()
        .HasForeignKey(c => c.ResponsibleMasterUserId);


      modelBuilder.Entity<Comment>().HasOptional(c => c.Parent).WithMany().WillCascadeOnDelete(false);
      modelBuilder.Entity<Comment>().HasRequired(comment => comment.CommentText).WithRequiredPrincipal();
      modelBuilder.Entity<CommentText>().HasKey(pd => pd.CommentId);
      modelBuilder.Entity<Comment>().HasRequired(comment => comment.Project).WithMany().WillCascadeOnDelete(false);
      modelBuilder.Entity<Comment>().HasRequired(comment => comment.Author).WithMany().WillCascadeOnDelete(false);
      modelBuilder.Entity<Comment>().HasRequired(c => c.Finance).WithRequiredPrincipal(fo => fo.Comment);

      modelBuilder.Entity<FinanceOperation>().HasKey(fo => fo.CommentId);

      modelBuilder.Entity<PlotFolder>().HasMany(pf => pf.RelatedGroups).WithMany(cg => cg.DirectlyRelatedPlotFolders);
      modelBuilder.Entity<PlotFolder>().HasRequired(pf => pf.Project).WithMany(p => p.PlotFolders).WillCascadeOnDelete(false);

      modelBuilder.Entity<PlotElement>().HasMany(pe => pe.TargetCharacters).WithMany(c => c.DirectlyRelatedPlotElements);
      modelBuilder.Entity<PlotElement>().HasMany(pe => pe.TargetGroups).WithMany(c => c.DirectlyRelatedPlotElements);
      modelBuilder.Entity<PlotElement>().HasRequired(pf => pf.Project).WithMany().WillCascadeOnDelete(false);

      modelBuilder.Entity<PlotElement>().HasMany(plotElement => plotElement.Texts).WithRequired(text => text.PlotElement);
      modelBuilder.Entity<PlotElementTexts>().HasKey(text => new {text.PlotElementId, text.Version});

      modelBuilder.Entity<PlotElementTexts>()
        .HasOptional(text => text.AuthorUser)
        .WithMany()
        .HasForeignKey(text => text.AuthorUserId);

      modelBuilder.Entity<User>().HasRequired(u => u.Auth).WithRequiredPrincipal();
      modelBuilder.Entity<UserAuthDetails>().HasKey(uad => uad.UserId);

      modelBuilder.Entity<User>().HasRequired(u => u.Allrpg).WithRequiredPrincipal();
      modelBuilder.Entity<AllrpgUserDetails>().HasKey(a => a.UserId);

      modelBuilder.Entity<User>().HasRequired(u => u.Extra).WithRequiredPrincipal();
      modelBuilder.Entity<UserExtra>().HasKey(a => a.UserId);

      modelBuilder.Entity<User>()
        .HasMany(u => u.ExternalLogins)
        .WithRequired(uel => uel.User)
        .HasForeignKey(uel => uel.UserId);
      
      modelBuilder.Entity<User>().HasMany(u => u.Subscriptions).WithRequired(s => s.User).WillCascadeOnDelete(true);
      modelBuilder.Entity<UserSubscription>().HasRequired(us => us.Project).WithMany().WillCascadeOnDelete(false);
      modelBuilder.Entity<UserSubscription>()
        .HasOptional(us => us.Claim)
        .WithMany(c => c.Subscriptions)
        .HasForeignKey(us => us.ClaimId)
        .WillCascadeOnDelete(false);

      modelBuilder.Entity<ProjectFieldDropdownValue>()
        .HasOptional(v => v.CharacterGroup)
        .WithOptionalDependent();

      modelBuilder.Entity<ProjectField>()
        .HasOptional(v => v.CharacterGroup)
        .WithOptionalDependent();

      modelBuilder.Entity<UserForumSubscription>().HasRequired(ufs => ufs.User).WithMany().WillCascadeOnDelete(false);

      modelBuilder.Entity<ProjectItemTag>().Property(tag => tag.TagName). IsUnique();
      modelBuilder.Entity<PlotFolder>().HasMany(tag => tag.PlotTags).WithMany();

      base.OnModelCreating(modelBuilder);
    }
  }
}
