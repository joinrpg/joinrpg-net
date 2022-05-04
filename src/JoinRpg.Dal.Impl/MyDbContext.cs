using JetBrains.Annotations;
using JoinRpg.Dal.Impl.Repositories;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Finances;
using JoinRpg.DataModel.Users;
using Microsoft.EntityFrameworkCore;

namespace JoinRpg.Dal.Impl
{
    [UsedImplicitly]
    public class MyDbContext : DbContext, IUnitOfWork
    {
        private readonly IJoinDbContextConfiguration _configuration;

        public MyDbContext(IJoinDbContextConfiguration configuration)
        {
            _configuration = configuration;
        }


        public DbSet<Project> ProjectsSet => Set<Project>();

        public DbSet<User> UserSet => Set<User>();

        public DbSet<Claim> ClaimSet => Set<Claim>();
        DbSet<T> IUnitOfWork.GetDbSet<T>() => Set<T>();

        Task IUnitOfWork.SaveChangesAsync() => SaveChangesAsync();

        public IUserRepository GetUsersRepository() => new UserInfoRepository(this);
        public IProjectRepository GetProjectRepository() => new ProjectRepository(this);

        public IClaimsRepository GetClaimsRepository() => new ClaimsRepositoryImpl(this);
        public IPlotRepository GetPlotRepository() => new PlotRepositoryImpl(this);
        public IForumRepository GetForumRepository() => new ForumRepositoryImpl(this);
        public ICharacterRepository GetCharactersRepository() => new CharacterRepositoryImpl(this);

        public IAccommodationRepository GetAccommodationRepository() =>
            new AccommodationRepositoryImpl(this);

        /// <inheritdoc />
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .EnableDetailedErrors(_configuration.DetailedErrors)
                .EnableSensitiveDataLogging(_configuration.SensitiveLogging)
                .UseNpgsql(_configuration.ConnectionString);
        }

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ConfigureProject(modelBuilder);

            ConfigurePlot(modelBuilder);

            ConfigureClaim(modelBuilder);

            ConfigureUser(modelBuilder);

            ConfigureAccommodation(modelBuilder);

            ConfigureFinance(modelBuilder);

            ConfigureForums(modelBuilder);

            base.OnModelCreating(modelBuilder);
        }

        private static void ConfigureUser(ModelBuilder mb)
        {
            mb.Entity<User>(
                eb =>
                {
                    eb.HasOne(e => e.Auth)
                        .WithOne()
                        .IsRequired();

                    eb.HasOne(e => e.Allrpg)
                        .WithOne()
                        .IsRequired();

                    eb.HasOne(e => e.Extra)
                        .WithOne()
                        .IsRequired();

                    eb.HasMany(u => u.ExternalLogins)
                        .WithOne(uel => uel.User)
                        .HasForeignKey(uel => uel.UserId);

                    eb.HasMany(u => u.Subscriptions)
                        .WithOne(s => s.User)
                        .HasForeignKey(e => e.UserId)
                        .OnDelete(DeleteBehavior.NoAction);

                    eb.HasMany(u => u.Avatars)
                        .WithOne(a => a.User)
                        .HasForeignKey(a => a.UserId)
                        .OnDelete(DeleteBehavior.NoAction);

                    eb.HasOne(c => c.SelectedAvatar)
                        .WithOne()
                        .HasForeignKey<User>(e => e.SelectedAvatarId)
                        .OnDelete(DeleteBehavior.SetNull);
                });

            mb.Entity<UserAuthDetails>()
                .HasKey(uad => uad.UserId);

            mb.Entity<AllrpgUserDetails>()
                .HasKey(a => a.UserId);

            mb.Entity<UserExtra>()
                .HasKey(a => a.UserId);

            mb.Entity<UserAvatar>(
                eb =>
                {
                    eb.HasKey(e => e.UserAvatarId);
                    eb.HasKey(e => e.UserId);
                });

            mb.Entity<UserSubscription>(
                eb =>
                {
                    eb.HasOne(e => e.Project)
                        .WithMany()
                        .HasForeignKey(e => e.ProjectId)
                        .OnDelete(DeleteBehavior.NoAction);

                    eb.HasOne(e => e.Claim)
                        .WithMany(e => e.Subscriptions)
                        .HasForeignKey(e => e.ClaimId)
                        .OnDelete(DeleteBehavior.NoAction);
                });

            mb.Entity<UserForumSubscription>(
                eb =>
                {
                    eb.HasOne(e => e.User)
                        .WithMany()
                        .HasForeignKey(e => e.UserId)
                        .OnDelete(DeleteBehavior.NoAction);
                });
        }

        private static void ConfigureProject(ModelBuilder mb)
        {
            mb.Entity<Project>(
                eb =>
                {
                    eb.HasOne(p => p.Details)
                        .WithOne()
                        .IsRequired();
                    eb.HasMany(p => p.Characters)
                        .WithOne(c => c.Project)
                        .HasForeignKey(e => e.ProjectId)
                        .OnDelete(DeleteBehavior.NoAction);
                });

            mb.Entity<ProjectDetails>()
                .HasKey(pd => pd.ProjectId);

            mb.Entity<ProjectAcl>(
                eb =>
                {
                    eb.HasKey(acl => acl.ProjectAclId);
                    eb.HasKey(c => new { c.UserId, c.ProjectId });
                });

            mb.Entity<ProjectFieldDropdownValue>(
                eb =>
                {
                    eb.HasOne(e => e.CharacterGroup)
                        .WithOne()
                        .IsRequired(false);
                });

            mb.Entity<ProjectField>(
                eb =>
                {
                    eb.HasOne(v => v.CharacterGroup)
                        .WithOne()
                        .IsRequired(false);
                });

            mb.Entity<ProjectItemTag>(
                eb =>
                {
                    eb.HasIndex(tag => tag.TagName)
                        .IsUnique();
                });
        }

        private static void ConfigurePlot(ModelBuilder mb)
        {
            mb.Entity<CharacterGroup>(
                eb =>
                {
                    eb.HasOne(e => e.ResponsibleMasterUser)
                        .WithMany()
                        .HasForeignKey(e => e.ResponsibleMasterUserId);
                    eb.HasMany(e => e.Characters)
                        .WithMany(e => e.Groups);

                });

            mb.Entity<Character>(
                eb =>
                {
                    eb.HasOne(e => e.ApprovedClaim)
                        .WithMany()
                        .HasForeignKey(e => e.ApprovedClaimId)
                        .OnDelete(DeleteBehavior.NoAction);

                });

            mb.Entity<PlotFolder>(
                eb =>
                {
                    eb.HasMany(e => e.RelatedGroups)
                        .WithMany(e => e.DirectlyRelatedPlotFolders);
                    eb.HasOne(e => e.Project)
                        .WithMany(e => e.PlotFolders)
                        .HasForeignKey(e => e.ProjectId)
                        .IsRequired()
                        .OnDelete(DeleteBehavior.NoAction);
                    eb.HasMany(e => e.PlotTags)
                        .WithMany(e => e.Folders);
                });

            mb.Entity<PlotElement>(
                eb =>
                {
                    eb.HasMany(e => e.TargetCharacters)
                        .WithMany(e => e.DirectlyRelatedPlotElements);
                    eb.HasMany(e => e.TargetGroups)
                        .WithMany(e => e.DirectlyRelatedPlotElements);
                    eb.HasOne(e => e.Project)
                        .WithMany()
                        .HasForeignKey(e => e.ProjectId)
                        .OnDelete(DeleteBehavior.NoAction);
                    eb.HasMany(e => e.Texts)
                        .WithOne(e => e.PlotElement)
                        .HasForeignKey(e => e.PlotElementId);
                });

            mb.Entity<PlotElementTexts>(
                eb =>
                {
                    eb.HasKey(e => new { e.PlotElementId, e.Version });
                    eb.HasOne(e => e.AuthorUser)
                        .WithMany()
                        .HasForeignKey(e => e.AuthorUserId);
                });
        }

        private static void ConfigureClaim(ModelBuilder mb)
        {
            mb.Entity<Claim>(
                eb =>
                {
                    eb.HasOne(e => e.Group)
                        .WithMany()
                        .HasForeignKey(e => e.CharacterGroupId)
                        .OnDelete(DeleteBehavior.NoAction);
                    eb.HasOne(e => e.Character)
                        .WithMany(e => e.Claims)
                        .HasForeignKey(e => e.CharacterId)
                        .OnDelete(DeleteBehavior.NoAction);
                    eb.HasOne(e => e.Player)
                        .WithMany(p => p.Claims)
                        .HasForeignKey(e => e.PlayerUserId)
                        .OnDelete(DeleteBehavior.NoAction);
                    eb.HasOne(e => e.Project)
                        .WithMany(e => e.Claims)
                        .HasForeignKey(e => e.ProjectId)
                        .OnDelete(DeleteBehavior.NoAction);
                    eb.HasOne(e => e.CommentDiscussion)
                        .WithMany()
                        .HasForeignKey(e => e.CommentDiscussionId)
                        .IsRequired()
                        .OnDelete(DeleteBehavior.NoAction);
                    eb.HasOne(e => e.ResponsibleMasterUser)
                        .WithMany()
                        .HasForeignKey(e => e.ResponsibleMasterUserId);
                    eb.HasMany(e => e.FinanceOperations)
                        .WithOne(e => e.Claim)
                        .HasForeignKey(e => e.ClaimId);
                });

            mb.Entity<CommentDiscussion>(
                eb =>
                {
                    eb.HasMany(e => e.Comments)
                        .WithOne(e => e.Discussion)
                        .HasForeignKey(e => e.CommentDiscussionId);
                });

            mb.Entity<Comment>(
                eb =>
                {
                    eb.HasOne(e => e.Parent)
                        .WithMany()
                        .HasForeignKey(e => e.ParentCommentId)
                        .OnDelete(DeleteBehavior.NoAction);
                    eb.HasOne(e => e.CommentText)
                        .WithOne()
                        .HasPrincipalKey<CommentText>(e => e.CommentId);
                    eb.HasOne(e => e.Project)
                        .WithMany()
                        .HasForeignKey(e => e.ProjectId)
                        .OnDelete(DeleteBehavior.NoAction);
                    eb.HasOne(e => e.Author)
                        .WithMany()
                        .HasForeignKey(e => e.AuthorUserId)
                        .OnDelete(DeleteBehavior.NoAction);
                    eb.HasOne(e => e.Finance)
                        .WithOne(e => e.Comment)
                        .HasPrincipalKey<FinanceOperation>(e => e.CommentId);
                });

            mb.Entity<CommentText>(
                eb =>
                {
                    eb.HasKey(e => e.CommentId);
                });
        }

        private static void ConfigureFinance(ModelBuilder mb)
        {
            mb.Entity<MoneyTransfer>(
                eb =>
                {
                    eb.HasOne(e => e.Sender)
                        .WithMany()
                        .HasForeignKey(e => e.SenderId)
                        .IsRequired()
                        .OnDelete(DeleteBehavior.NoAction);
                    eb.HasOne(e => e.Receiver)
                        .WithMany()
                        .HasForeignKey(e => e.ReceiverId)
                        .IsRequired()
                        .OnDelete(DeleteBehavior.NoAction);
                    eb.HasOne(e => e.CreatedBy)
                        .WithMany()
                        .HasForeignKey(e => e.CreatedById)
                        .IsRequired()
                        .OnDelete(DeleteBehavior.NoAction);
                    eb.HasOne(e => e.ChangedBy)
                        .WithMany()
                        .HasForeignKey(e => e.ChangedById)
                        .IsRequired()
                        .OnDelete(DeleteBehavior.NoAction);

                    eb.HasOne(e => e.TransferText)
                        .WithOne()
                        .HasForeignKey<TransferText>(e => e.MoneyTransferId)
                        .IsRequired();
                });

            mb.Entity<TransferText>(
                eb =>
                {
                    eb.HasKey(e => e.MoneyTransferId);
                });

            mb.Entity<FinanceOperation>(
                eb =>
                {
                    eb.HasKey(fo => fo.CommentId);

                    eb.HasOne(e => e.LinkedClaim)
                        .WithMany(e => e.FinanceOperations)
                        .HasForeignKey(e => e.LinkedClaimId);
                });
        }

        private static void ConfigureAccommodation(ModelBuilder mb)
        {
            mb.Entity<AccommodationRequest>(
                eb =>
                {
                    eb.HasMany(e => e.Subjects)
                        .WithOne(e => e.AccommodationRequest)
                        .HasForeignKey(e => e.AccommodationRequestId);
                });

            mb.Entity<ProjectAccommodationType>();

            mb.Entity<ProjectAccommodation>();

            mb.Entity<AccommodationRequest>();

            mb.Entity<AccommodationInvite>();
        }

        private static void ConfigureForums(ModelBuilder mb)
        {
            mb.Entity<ForumThread>(
                eb =>
                {
                    eb.HasOne(e => e.CommentDiscussion)
                        .WithMany()
                        .HasForeignKey(e => e.CommentDiscussionId)
                        .OnDelete(DeleteBehavior.NoAction);
                });
        }
    }
}
