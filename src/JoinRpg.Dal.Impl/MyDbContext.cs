using System.Data.Entity;
using JoinRpg.Dal.Impl.Repositories;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Finances;
using Microsoft.Extensions.Logging;

namespace JoinRpg.Dal.Impl;

[DbConfigurationType(typeof(MyDbConfiguration))]
public class MyDbContext : DbContext, IUnitOfWork
{
    public DbSet<Project> ProjectsSet => Set<Project>();

    public DbSet<User> UserSet => Set<User>();

    public DbSet<Claim> ClaimSet => Set<Claim>();

    internal ILogger<MyDbContext>? Logger { get; }

    public MyDbContext(IJoinDbContextConfiguration configuration, ILogger<MyDbContext>? logger) : base(configuration.ConnectionString)
    {
        Logger = logger;
        if (logger is not null)
        {
            // На самом деле EF6LoggerToMSExtLogging перехватывает эти вызовы и сам пишет в логгер структурно, но если не вызвать Database.set_Log, то он не активируется
            Database.Log = (message) => logger.LogDebug("{message}", message);
        }
    }

    DbSet<T> IUnitOfWork.GetDbSet<T>() => Set<T>();

    Task IUnitOfWork.SaveChangesAsync() => SaveChangesAsync();

    public IUserRepository GetUsersRepository() => new UserInfoRepository(this);
    public IProjectRepository GetProjectRepository() => new ProjectRepository(this);

    public IClaimsRepository GetClaimsRepository() => new ClaimsRepositoryImpl(this);
    public IPlotRepository GetPlotRepository() => new PlotRepositoryImpl(this);
    public IForumRepository GetForumRepository() => new ForumRepositoryImpl(this);
    public ICharacterRepository GetCharactersRepository() => new CharacterRepositoryImpl(this);

    public IAccommodationRepository GetAccomodationRepository() =>
        new AccommodationRepositoryImpl(this);

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
        ConfigureProjectDetails(modelBuilder);

        _ = modelBuilder.Entity<ProjectAcl>().HasKey(c => new { c.UserId, c.ProjectId });
        _ = modelBuilder.Entity<ProjectAcl>().HasKey(acl => acl.ProjectAclId);

        _ = modelBuilder.Entity<CharacterGroup>()
            .HasOptional(c => c.ResponsibleMasterUser)
            .WithMany()
            .HasForeignKey(c => c.ResponsibleMasterUserId);


        modelBuilder.Entity<Project>().HasMany(p => p.Characters).WithRequired(c => c.Project)
            .WillCascadeOnDelete(false);

        modelBuilder.Entity<Claim>().HasRequired(c => c.Character).WithMany(c => c.Claims)
            .HasForeignKey(c => c.CharacterId).WillCascadeOnDelete(false);
        modelBuilder.Entity<Claim>().HasRequired(c => c.Player).WithMany(p => p.Claims)
            .WillCascadeOnDelete(false);
        modelBuilder.Entity<Claim>().HasRequired(c => c.Project).WithMany(p => p.Claims)
            .WillCascadeOnDelete(false);

        modelBuilder.Entity<Character>().HasOptional(c => c.ApprovedClaim).WithMany()
            .HasForeignKey(c => c.ApprovedClaimId).WillCascadeOnDelete(false);

        _ = modelBuilder.Entity<CommentDiscussion>().HasMany(c => c.Comments)
            .WithRequired(c => c.Discussion);

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

        _ = modelBuilder.Entity<Claim>()
            .HasRequired(c => c.ResponsibleMasterUser)
            .WithMany()
            .HasForeignKey(c => c.ResponsibleMasterUserId);

        _ = modelBuilder.Entity<Claim>()
            .HasMany(c => c.FinanceOperations)
            .WithRequired(fo => fo.Claim)
            .HasForeignKey(fo => fo.ClaimId);

        _ = modelBuilder.Entity<AccommodationRequest>().HasMany(c => c.Subjects)
            .WithOptional(c => c.AccommodationRequest!);


        modelBuilder.Entity<Comment>().HasOptional(c => c.Parent).WithMany()
            .WillCascadeOnDelete(false);
        _ = modelBuilder.Entity<Comment>().HasRequired(comment => comment.CommentText)
            .WithRequiredPrincipal();
        _ = modelBuilder.Entity<CommentText>().HasKey(pd => pd.CommentId);
        modelBuilder.Entity<Comment>().HasRequired(comment => comment.Project).WithMany()
            .WillCascadeOnDelete(false);
        modelBuilder.Entity<Comment>().HasRequired(comment => comment.Author).WithMany()
            .WillCascadeOnDelete(false);
        _ = modelBuilder.Entity<Comment>().HasRequired(c => c.Finance)
            .WithRequiredPrincipal(fo => fo.Comment);

        ConfigureFinanceOperation(modelBuilder);
        ConfigureRecurrentPayments(modelBuilder);

        _ = modelBuilder.Entity<PlotFolder>().HasMany(pf => pf.RelatedGroups)
            .WithMany(cg => cg.DirectlyRelatedPlotFolders);
        modelBuilder.Entity<PlotFolder>().HasRequired(pf => pf.Project)
            .WithMany(p => p.PlotFolders).WillCascadeOnDelete(false);

        _ = modelBuilder.Entity<PlotElement>().HasMany(pe => pe.TargetCharacters)
            .WithMany(c => c.DirectlyRelatedPlotElements);
        _ = modelBuilder.Entity<PlotElement>().HasMany(pe => pe.TargetGroups)
            .WithMany(c => c.DirectlyRelatedPlotElements);
        modelBuilder.Entity<PlotElement>().HasRequired(pf => pf.Project).WithMany()
            .WillCascadeOnDelete(false);

        _ = modelBuilder.Entity<PlotElement>().HasMany(plotElement => plotElement.Texts)
            .WithRequired(text => text.PlotElement);
        _ = modelBuilder.Entity<PlotElementTexts>()
            .HasKey(text => new { text.PlotElementId, text.Version });

        _ = modelBuilder.Entity<PlotElementTexts>()
            .HasOptional(text => text.AuthorUser)
            .WithMany()
            .HasForeignKey(text => text.AuthorUserId);
        ConfigureUser(modelBuilder);

        _ = modelBuilder.Entity<ProjectFieldDropdownValue>()
            .HasOptional(v => v.CharacterGroup)
            .WithOptionalDependent();

        _ = modelBuilder.Entity<ProjectField>()
            .HasOptional(v => v.CharacterGroup)
            .WithOptionalDependent();

        modelBuilder.Entity<UserForumSubscription>().HasRequired(ufs => ufs.User).WithMany()
            .WillCascadeOnDelete(false);

        modelBuilder.Entity<ProjectItemTag>().Property(tag => tag.TagName).IsUnique();
        _ = modelBuilder.Entity<PlotFolder>().HasMany(tag => tag.PlotTags).WithMany();

        _ = modelBuilder.Entity<ProjectAccommodationType>();
        _ = modelBuilder.Entity<ProjectAccommodation>();
        _ = modelBuilder.Entity<AccommodationRequest>();
        _ = modelBuilder.Entity<AccommodationInvite>();

        ConfigureMoneyTransfer(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    private void ConfigureRecurrentPayments(DbModelBuilder modelBuilder)
    {
        ConfigureProjectSubEntity<RecurrentPayment>(modelBuilder);

        modelBuilder.Entity<RecurrentPayment>()
            .HasRequired(rp => rp.Claim)
            .WithMany(e => e.RecurrentPayments)
            .HasForeignKey(rp => rp.ClaimId)
            .WillCascadeOnDelete(false);

        modelBuilder.Entity<RecurrentPayment>()
            .HasRequired(rp => rp.PaymentType)
            .WithMany()
            .HasForeignKey(rp => rp.PaymentTypeId)
            .WillCascadeOnDelete(false);
    }

    private static void ConfigureProjectSubEntity<TEntity>(DbModelBuilder modelBuilder)
        where TEntity : class, IProjectEntity
    {
        modelBuilder.Entity<TEntity>()
                    .HasRequired(rp => rp.Project)
                    .WithMany()
                    .HasForeignKey(rp => rp.ProjectId)
                    .WillCascadeOnDelete(false);
    }

    private static void ConfigureFinanceOperation(DbModelBuilder modelBuilder)
    {
        _ = modelBuilder.Entity<FinanceOperation>().HasKey(fo => fo.CommentId);

        _ = modelBuilder.Entity<FinanceOperation>()
            .HasOptional(fo => fo.LinkedClaim)
            .WithMany()
            .HasForeignKey(fo => fo.LinkedClaimId);

        _ = modelBuilder.Entity<FinanceOperation>()
            .HasOptional(fo => fo.RecurrentPayment)
            .WithMany()
            .HasForeignKey(fo => fo.RecurrentPaymentId);

        _ = modelBuilder.Entity<FinanceOperation>()
            .HasOptional(fo => fo.RefundedOperation)
            .WithMany()
            .HasForeignKey(fo => fo.RefundedOperationId);

        _ = modelBuilder.Entity<FinanceOperationBankDetails>()
            .HasKey(fobd => fobd.CommentId);

        _ = modelBuilder.Entity<FinanceOperation>()
            .HasOptional(fo => fo.BankDetails)
            .WithRequired();
    }

    private static void ConfigureUser(DbModelBuilder modelBuilder)
    {
        _ = modelBuilder.Entity<User>().HasRequired(u => u.Auth).WithRequiredPrincipal();
        _ = modelBuilder.Entity<UserAuthDetails>().HasKey(uad => uad.UserId);

        _ = modelBuilder.Entity<User>().HasRequired(u => u.Allrpg).WithRequiredPrincipal();
        _ = modelBuilder.Entity<AllrpgUserDetails>().HasKey(a => a.UserId);

        _ = modelBuilder.Entity<User>().HasRequired(u => u.Extra).WithRequiredPrincipal();
        _ = modelBuilder.Entity<UserExtra>().HasKey(a => a.UserId);

        _ = modelBuilder.Entity<User>()
            .HasMany(u => u.ExternalLogins)
            .WithRequired(uel => uel.User)
            .HasForeignKey(uel => uel.UserId);

        modelBuilder.Entity<User>().HasMany(u => u.Subscriptions).WithRequired(s => s.User)
            .WillCascadeOnDelete(true);
        modelBuilder.Entity<UserSubscription>().HasRequired(us => us.Project).WithMany()
            .WillCascadeOnDelete(false);
        modelBuilder.Entity<UserSubscription>()
            .HasOptional(us => us.Claim)
            .WithMany(c => c.Subscriptions)
            .HasForeignKey(us => us.ClaimId)
            .WillCascadeOnDelete(false);

        modelBuilder.Entity<User>()
            .HasMany(u => u.Avatars)
            .WithRequired(a => a.User)
            .HasForeignKey(a => a.UserId)
            .WillCascadeOnDelete(false);

        modelBuilder.Entity<User>()
            .HasOptional(c => c.SelectedAvatar)
            .WithMany()
            .WillCascadeOnDelete(false);
    }

    private static void ConfigureProjectDetails(DbModelBuilder modelBuilder)
    {
        _ = modelBuilder.Entity<Project>().HasRequired(p => p.Details).WithRequiredPrincipal();
        _ = modelBuilder.Entity<ProjectDetails>().HasKey(pd => pd.ProjectId);
    }

    private static void ConfigureMoneyTransfer(DbModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<MoneyTransfer>();
        entity.HasRequired(e => e.Sender).WithMany().WillCascadeOnDelete(false);
        entity.HasRequired(e => e.Receiver).WithMany().WillCascadeOnDelete(false);
        entity.HasRequired(e => e.CreatedBy).WithMany().WillCascadeOnDelete(false);
        entity.HasRequired(e => e.ChangedBy).WithMany().WillCascadeOnDelete(false);

        _ = entity.HasRequired(comment => comment.TransferText).WithRequiredPrincipal();
        _ = modelBuilder.Entity<TransferText>().HasKey(pd => pd.MoneyTransferId);
    }
}
