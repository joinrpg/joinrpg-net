using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Interfaces;

namespace JoinRpg.Services.Impl;

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
        _accomodationRepository = new Lazy<IAccommodationRepository>(UnitOfWork.GetAccommodationRepository);
        _charactersRepository =
            new Lazy<ICharacterRepository>(unitOfWork.GetCharactersRepository);

        Now = DateTime.UtcNow;
    }

    protected void StartImpersonate(int userId) => _impersonatedUserId = userId;

    protected void ResetImpersonation() => _impersonatedUserId = null;

    protected async Task<T> LoadProjectSubEntityAsync<T>(int projectId, int subentityId)
        where T : class, IProjectEntity
    {
        var field = await UnitOfWork.GetDbSet<T>().FindAsync(subentityId);
        if (field != null && field.Project.ProjectId == projectId)
        {
            return field;
        }

            throw new JoinRpgEntityNotFoundException(subentityId, typeof(T).Name);
        }

        protected static string Required([NotNull] string? stringValue, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(stringValue))
            {
                throw new FieldRequiredException(fieldName);
            }

        return stringValue.Trim();
    }

        protected static string Required(Expression<Func<string>> itemsLambda)
        {
            var name = itemsLambda.AsPropertyName() ?? throw new InvalidOperationException();
            var stringValue = itemsLambda.Compile()();
            if (string.IsNullOrWhiteSpace(stringValue))
            {
                throw new FieldRequiredException(name);
            }

            return stringValue;
        }

        protected static IReadOnlyCollection<T> Required<T>(IReadOnlyCollection<T> items, string fieldName)
        {
            if (items.Count == 0)
            {
                throw new FieldRequiredException(fieldName);
            }

        return items;
    }

        protected static IReadOnlyCollection<T> Required<T>(
            Expression<Func<IReadOnlyCollection<T>>> itemsLambda)
        {
            var name = itemsLambda.AsPropertyName() ?? throw new InvalidOperationException();
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
                throw new ValidationException();
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
                throw new JoinRpgEntityNotFoundException(characterIds.Except(characters.Select(c => c.CharacterId)), "character");
            }

        return characters.ToArray();
    }

    protected void MarkCreatedNow(ICreatedUpdatedTrackedForEntity entity)
    {
        entity.UpdatedAt = entity.CreatedAt = Now;
        entity.UpdatedById = entity.CreatedById = CurrentUserId;
    }

    protected void Create<T>(T entity)
        where T : class, ICreatedUpdatedTrackedForEntity
    {
        MarkCreatedNow(entity);

        _ = UnitOfWork.GetDbSet<T>().Add(entity);
    }

    protected void MarkChanged(ICreatedUpdatedTrackedForEntity entity)
    {
        entity.UpdatedAt = Now;
        entity.UpdatedById = CurrentUserId;
    }

    protected void MarkTreeModified(Project project) => project.CharacterTreeModifiedAt = Now;

    protected async Task<User> GetCurrentUser() => await UserRepository.GetById(CurrentUserId);
}
