using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Interfaces;

namespace JoinRpg.Services.Impl
{
    //TODO: Split on specific and not specific to domain helpers
    public class DbServiceImplBase
    {
        protected readonly IUnitOfWork UnitOfWork;
        private readonly ICurrentUserAccessor currentUserAccessor;

        protected IUserRepository UserRepository => _userRepository.Value;

        private readonly Lazy<IUserRepository> _userRepository;

        protected IAccommodationRepository AccomodationRepository => _accomodationRepository.Value;
        private readonly Lazy<IAccommodationRepository> _accomodationRepository;

        protected IProjectRepository ProjectRepository => _projectRepository.Value;

        private readonly Lazy<IProjectRepository> _projectRepository;

        protected IClaimsRepository ClaimsRepository => _claimRepository.Value;
        private readonly Lazy<IClaimsRepository> _claimRepository;

        protected IForumRepository ForumRepository => _forumRepository.Value;
        private readonly Lazy<IForumRepository> _forumRepository;

        private readonly Lazy<IPlotRepository> _plotRepository;
        protected IPlotRepository PlotRepository => _plotRepository.Value;

        private readonly Lazy<ICharacterRepository> _charactersRepository;
        protected ICharacterRepository CharactersRepository => _charactersRepository.Value;

        private int? _impersonatedUserId;

        /// <summary>
        /// Returns current user database Id
        /// </summary>
        protected int CurrentUserId => _impersonatedUserId ?? currentUserAccessor.UserId;
        // TODO: Fix impersonation

        /// <summary>
        /// Returns true if current user is admin
        /// </summary>
        protected bool IsCurrentUserAdmin => currentUserAccessor.IsAdmin;

        /// <summary>
        /// Time of service creation. Used to mark consistent time for all operations performed by service
        /// </summary>
        protected DateTime Now { get; }

        protected DbServiceImplBase(IUnitOfWork unitOfWork, ICurrentUserAccessor currentUserAccessor)
        {
            UnitOfWork = unitOfWork;
            this.currentUserAccessor = currentUserAccessor;
            _userRepository = new Lazy<IUserRepository>(unitOfWork.GetUsersRepository);
            _projectRepository = new Lazy<IProjectRepository>(unitOfWork.GetProjectRepository);
            _claimRepository = new Lazy<IClaimsRepository>(unitOfWork.GetClaimsRepository);
            _plotRepository = new Lazy<IPlotRepository>(unitOfWork.GetPlotRepository);
            _forumRepository = new Lazy<IForumRepository>(unitOfWork.GetForumRepository);
            _accomodationRepository = new Lazy<IAccommodationRepository>(UnitOfWork.GetAccomodationRepository);
            _charactersRepository =
                new Lazy<ICharacterRepository>(unitOfWork.GetCharactersRepository);

            Now = DateTime.UtcNow;
        }

        protected void StartImpersonate(int userId) => _impersonatedUserId = userId;

        protected void ResetImpersonation() => _impersonatedUserId = null;

        [NotNull]
        protected async Task<T> LoadProjectSubEntityAsync<T>(int projectId, int subentityId)
            where T : class, IProjectEntity
        {
            var field = await UnitOfWork.GetDbSet<T>().FindAsync(subentityId);
            if (field != null && field.Project.ProjectId == projectId)
            {
                return field;
            }

            throw new DbEntityValidationException();
        }

        protected static string Required(string stringValue)
        {
            if (string.IsNullOrWhiteSpace(stringValue))
            {
                throw new DbEntityValidationException();
            }

            return stringValue.Trim();
        }

        protected static IReadOnlyCollection<T> Required<T>(IReadOnlyCollection<T> items)
        {
            if (items.Count == 0)
            {
                throw new DbEntityValidationException();
            }

            return items;
        }

        protected static IReadOnlyCollection<T> Required<T>(
            Expression<Func<IReadOnlyCollection<T>>> itemsLambda)
        {
            var name = itemsLambda.AsPropertyName();
            var items = itemsLambda.Compile()();
            if (items.Count == 0)
            {
                throw new FieldRequiredException(name);
            }

            return items;
        }

        protected bool SmartDelete<T>(T field) where T : class, IDeletableSubEntity
        {
            if (field == null)
            {
                return false;
            }

            if (field.CanBePermanentlyDeleted)
            {
                _ = UnitOfWork.GetDbSet<T>().Remove(field);
                return true;
            }
            else
            {
                field.IsActive = false;
                return false;
            }
        }

        [ItemNotNull]
        protected async Task<int[]> ValidateCharacterGroupList(int projectId,
            IReadOnlyCollection<int> groupIds,
            bool ensureNotSpecial = false)
        {
            var characterGroups = await ProjectRepository.LoadGroups(projectId, groupIds);

            if (characterGroups.Count != groupIds.Distinct().Count())
            {
                var missing = string.Join(", ",
                    groupIds.Except(characterGroups.Select(cg => cg.CharacterGroupId)));
                throw new Exception($"Groups {missing} doesn't belong to project");
            }

            if (ensureNotSpecial && characterGroups.Any(cg => cg.IsSpecial))
            {
                throw new DbEntityValidationException();
            }

            return groupIds.ToArray();
        }

        protected async Task<ICollection<Character>> ValidateCharactersList(int projectId,
            IReadOnlyCollection<int> characterIds)
        {
            var characters =
                await CharactersRepository.GetCharacters(projectId, characterIds);

            if (characters.Count != characterIds.Distinct().Count())
            {
                throw new DbEntityValidationException();
            }

            return characters.ToArray();
        }

        protected void MarkCreatedNow([NotNull] ICreatedUpdatedTrackedForEntity entity)
        {
            entity.UpdatedAt = entity.CreatedAt = Now;
            entity.UpdatedById = entity.CreatedById = CurrentUserId;
        }

        protected void Create<T>([NotNull] T entity)
            where T : class, ICreatedUpdatedTrackedForEntity
        {
            MarkCreatedNow(entity);

            _ = UnitOfWork.GetDbSet<T>().Add(entity);
        }

        protected void MarkChanged([NotNull] ICreatedUpdatedTrackedForEntity entity)
        {
            entity.UpdatedAt = Now;
            entity.UpdatedById = CurrentUserId;
        }

        protected void MarkTreeModified([NotNull] Project project) => project.CharacterTreeModifiedAt = Now;

        protected async Task<User> GetCurrentUser() => await UserRepository.GetById(CurrentUserId);
    }
}
