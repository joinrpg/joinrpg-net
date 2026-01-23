using System.Data.Entity.Validation;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;

namespace JoinRpg.Services.Impl;

//TODO: Split on specific and not specific to domain helpers
public class DbServiceImplBase
{
    protected readonly IUnitOfWork UnitOfWork;
    protected readonly ICurrentUserAccessor currentUserAccessor;

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

    /// <summary>
    /// Returns current user database Id
    /// </summary>
    protected int CurrentUserId => currentUserAccessor.UserId;

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

    protected async Task<T> LoadProjectSubEntityAsync<T>(IProjectEntityId id)
    where T : class, IProjectEntity
    {
        var field = await UnitOfWork.GetDbSet<T>().FindAsync(id.Id);
        if (field != null && field.Project.ProjectId == id.ProjectId)
        {
            return field;
        }

        throw new DbEntityValidationException();
    }

    protected static string Required([NotNull] string? stringValue,
        [CallerArgumentExpression(nameof(stringValue))] string? fieldName = null)
    {
        if (string.IsNullOrWhiteSpace(stringValue))
        {
            throw new FieldRequiredException(fieldName ?? "??unknown field");
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

    protected bool SmartDelete<T>(T? field) where T : class, IDeletableSubEntity
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

    protected async Task<int[]> ValidateCharacterGroupList(ProjectIdentification projectId,
       IReadOnlyCollection<CharacterGroupIdentification> groupIds,
       bool ensureNotSpecial = false)
    {
        foreach (var g in groupIds)
        {
            if (g.ProjectId != projectId)
            {
                throw new ArgumentException("Нельзя смешивать разные проекты в запросе!", nameof(groupIds));
            }
        }
        return await ValidateCharacterGroupList(projectId.Value, [.. groupIds.Select(g => g.CharacterGroupId)], ensureNotSpecial);
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
            throw new DbEntityValidationException();
        }

        return groupIds.ToArray();
    }

    protected async Task<ICollection<Character>> ValidateCharactersList(IReadOnlyCollection<CharacterIdentification> characterIds)
    {
        if (characterIds.Count == 0)
        {
            return [];
        }
        if (characterIds.Select(c => c.ProjectId).Distinct().Count() > 1)
        {
            throw new ArgumentException("Нельзя смешивать разные проекты в запросе!", nameof(characterIds));
        }
        var characters =
            await CharactersRepository.GetCharacters(characterIds);

        if (characters.Count != characterIds.Distinct().Count())
        {
            throw new DbEntityValidationException();
        }

        return characters.ToArray();
    }

    protected void MarkCreatedNow(ICreatedUpdatedTrackedForEntity entity)
    {
        entity.UpdatedAt = entity.CreatedAt = Now;
        entity.UpdatedById = entity.CreatedById = CurrentUserId;
    }

    protected T Create<T>(T entity)
        where T : class, ICreatedUpdatedTrackedForEntity
    {
        MarkCreatedNow(entity);

        _ = UnitOfWork.GetDbSet<T>().Add(entity);
        return entity;
    }

    protected void MarkChanged(ICreatedUpdatedTrackedForEntity entity)
    {
        entity.UpdatedAt = Now;
        entity.UpdatedById = CurrentUserId;
    }

    protected void MarkTreeModified(Project project) => project.CharacterTreeModifiedAt = Now;

    protected async Task<User> GetCurrentUser() => await UserRepository.GetById(CurrentUserId);
}
