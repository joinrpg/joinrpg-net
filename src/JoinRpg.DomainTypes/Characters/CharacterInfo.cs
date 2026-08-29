using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.DomainTypes.Characters;

/// <summary>
/// Доменный агрегат персонажа (ADR013) — аналог <see cref="ProjectMetadata.ProjectInfo"/>, но для
/// персонажа. Несёт полную правду о нём: настройки, слой полей и базовые сведения обо всех заявках.
/// </summary>
/// <remarks>
/// <para>
/// Тип user-independent: <see cref="AccessArguments"/> внутрь не входят, персонаж хранится
/// «как есть». Фильтрация по доступу делается снаружи — через <see cref="GetFieldLayers"/>.
/// </para>
/// <para>
/// ВАЖНО: экземпляр привязан к конкретному экземпляру <see cref="ProjectInfo"/> (это проверяется
/// в конструкторе). Поэтому кешировать <see cref="CharacterInfo"/> между запросами без синхронной
/// инвалидации обоих нельзя.
/// </para>
/// <para>
/// Полных данных о комментариях и о сюжетах здесь нет — это отдельные агрегаты.
/// </para>
/// </remarks>
public record class CharacterInfo : IFieldAvailabilityTarget
{
    private readonly Lazy<IReadOnlyCollection<CharacterGroupIdentification>> parentGroupIdsToTop;
    private readonly Lazy<int> activeClaimsCount;
    private readonly Lazy<UserIdentification> responsibleMasterId;

    public CharacterIdentification Id { get; }
    public ProjectInfo ProjectInfo { get; }

    public string CharacterName { get; }
    public CharacterTypeInfo CharacterTypeInfo { get; }

    /// <summary>
    /// Скрывать ли игрока на странице персонажа. Хранится отдельно от
    /// <see cref="CharacterTypeInfo"/>, потому что <see cref="CharacterVisibility"/> схлопывает
    /// пару флагов: при непубличном персонаже исходное значение из него не восстановить.
    /// </summary>
    public bool HidePlayerForCharacter { get; }

    public bool IsActive { get; }
    public bool InGame { get; }
    public bool AutoCreated { get; }

    /// <summary>Краткое описание, используется в дереве ролей.</summary>
    public MarkdownString Description { get; }

    /// <summary>Слот, из которого создан этот персонаж, если он создан из слота.</summary>
    public CharacterIdentification? OriginalCharacterSlotId { get; }

    /// <summary>Группы, в которых персонаж состоит напрямую.</summary>
    public IReadOnlyCollection<CharacterGroupIdentification> DirectGroupIds { get; }

    /// <summary>Слой значений полей самого персонажа (без слоя заявки).</summary>
    public FieldLayerContainer CharacterFields { get; }

    /// <summary>Все заявки на этого персонажа, включая отклонённые.</summary>
    public IReadOnlyCollection<CharacterClaimInfo> Claims { get; }

    public ClaimIdentification? ApprovedClaimId { get; }

    public DateTime CreatedAt { get; }
    public UserIdentification CreatedById { get; }
    public DateTime UpdatedAt { get; }
    public UserIdentification UpdatedById { get; }

    public CharacterInfo(
        CharacterIdentification id,
        ProjectInfo projectInfo,
        string characterName,
        CharacterTypeInfo characterTypeInfo,
        bool hidePlayerForCharacter,
        bool isActive,
        bool inGame,
        bool autoCreated,
        MarkdownString description,
        CharacterIdentification? originalCharacterSlotId,
        IReadOnlyCollection<CharacterGroupIdentification> directGroupIds,
        FieldLayerContainer characterFields,
        IReadOnlyCollection<CharacterClaimInfo> claims,
        ClaimIdentification? approvedClaimId,
        DateTime createdAt,
        UserIdentification createdById,
        DateTime updatedAt,
        UserIdentification updatedById)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(projectInfo);
        ArgumentNullException.ThrowIfNull(characterFields);
        ArgumentNullException.ThrowIfNull(claims);

        if (id.ProjectId != projectInfo.ProjectId)
        {
            throw new ArgumentException(
                $"Character {id} does not belong to project {projectInfo.ProjectId}", nameof(id));
        }

        if (!ReferenceEquals(characterFields.ProjectInfo, projectInfo))
        {
            throw new ArgumentException(
                "Character field layer is bound to another ProjectInfo instance", nameof(characterFields));
        }

        foreach (var claim in claims)
        {
            if (claim.ClaimId.ProjectId != projectInfo.ProjectId)
            {
                throw new ArgumentException(
                    $"Claim {claim.ClaimId} does not belong to project {projectInfo.ProjectId}", nameof(claims));
            }

            if (!ReferenceEquals(claim.Fields.ProjectInfo, projectInfo))
            {
                throw new ArgumentException(
                    $"Field layer of claim {claim.ClaimId} is bound to another ProjectInfo instance", nameof(claims));
            }
        }

        var approvedClaims = claims.Where(c => c.IsApproved).ToArray();
        if (approvedClaims.Length > 1)
        {
            throw new ArgumentException($"Character {id} has more than one approved claim", nameof(claims));
        }

        CharacterClaimInfo? approvedClaim = null;
        if (approvedClaimId is not null)
        {
            approvedClaim = claims.SingleOrDefault(c => c.ClaimId == approvedClaimId)
                ?? throw new ArgumentException(
                    $"Approved claim {approvedClaimId} is not among claims of character {id}", nameof(approvedClaimId));

            if (!approvedClaim.IsApproved)
            {
                throw new ArgumentException(
                    $"Claim {approvedClaimId} is marked as approved on character {id}, but has status {approvedClaim.Status}",
                    nameof(approvedClaimId));
            }
        }

        Id = id;
        ProjectInfo = projectInfo;
        CharacterName = characterName;
        CharacterTypeInfo = characterTypeInfo;
        HidePlayerForCharacter = hidePlayerForCharacter;
        IsActive = isActive;
        InGame = inGame;
        AutoCreated = autoCreated;
        Description = description;
        OriginalCharacterSlotId = originalCharacterSlotId;
        DirectGroupIds = directGroupIds;
        CharacterFields = characterFields;
        Claims = claims;
        ApprovedClaimId = approvedClaimId;
        ApprovedClaim = approvedClaim;
        CreatedAt = createdAt;
        CreatedById = createdById;
        UpdatedAt = updatedAt;
        UpdatedById = updatedById;

        parentGroupIdsToTop = new(() => [.. projectInfo.GetParentGroupIdsIncludingThis(directGroupIds)]);
        activeClaimsCount = new(() => claims.Count(c => c.IsActive));
        responsibleMasterId = new(
            () => ApprovedClaim?.ResponsibleMasterId
                ?? projectInfo.SelectResponsibleMaster(ParentGroupIdsToTop).UserId);
    }

    public bool IsPublic => CharacterTypeInfo.IsPublic;

    public CharacterType CharacterType => CharacterTypeInfo.CharacterType;

    /// <summary>Утверждённая заявка, если она есть.</summary>
    public CharacterClaimInfo? ApprovedClaim { get; }

    public IEnumerable<CharacterClaimInfo> ActiveClaims => Claims.Where(c => c.IsActive);

    public int ActiveClaimsCount => activeClaimsCount.Value;

    public bool HasActiveClaims => ActiveClaimsCount > 0;

    /// <summary>Все группы персонажа вверх до корня, включая прямые.</summary>
    public IReadOnlyCollection<CharacterGroupIdentification> ParentGroupIdsToTop => parentGroupIdsToTop.Value;

    public IEnumerable<CharacterGroupInfo> ParentGroupsToTop
        => ParentGroupIdsToTop.Select(ProjectInfo.GetGroupById);

    public IEnumerable<CharacterGroupInfo> DirectGroups => ProjectInfo.GetGroupsById(DirectGroupIds);

    /// <summary>Группы, которые имеет смысл показывать пользователю рядом с персонажем.</summary>
    public IEnumerable<CharacterGroupInfo> IntrestingGroupsForDisplay
        => ParentGroupsToTop.Where(g => g.IsIntresting);

    /// <summary>
    /// Ответственный мастер: указанный в утверждённой заявке, иначе выбранный по правилам групп.
    /// </summary>
    public UserIdentification ResponsibleMasterId => responsibleMasterId.Value;

    public CharacterClaimInfo GetClaimById(ClaimIdentification claimId)
        => Claims.SingleOrDefault(c => c.ClaimId == claimId)
            ?? throw new KeyNotFoundException($"Claim {claimId} not found on character {Id}");

    /// <summary>
    /// Собирает послойное представление полей для конкретного зрителя.
    /// </summary>
    /// <param name="access">Права зрителя — по ним фильтруются поля.</param>
    /// <param name="claimId">
    /// Заявка, чей слой накладывается поверх слоя персонажа. Если не задана — берётся утверждённая
    /// заявка (или только слой персонажа, если утверждённой нет).
    /// </param>
    /// <exception cref="KeyNotFoundException">Заявки <paramref name="claimId"/> нет у этого персонажа.</exception>
    public CharacterFieldLayers GetFieldLayers(AccessArguments access, ClaimIdentification? claimId = null)
    {
        var claim = claimId is null ? ApprovedClaim : GetClaimById(claimId);
        return new CharacterFieldLayers(claim?.Fields, CharacterFields, access);
    }

    /// <summary>
    /// Полный набор полей — запись для каждого поля проекта, без фильтрации по доступу.
    /// Для расчётов (взносы, проблемы), а не для показа: для показа нужен
    /// <see cref="GetFieldLayers"/> с правами конкретного зрителя.
    /// </summary>
    /// <param name="claimId">См. <see cref="GetFieldLayers"/>.</param>
    public IReadOnlyCollection<FieldWithValue> GetAllFields(ClaimIdentification? claimId = null)
        // AccessArguments.None здесь безвреден: GetAllFieldsForEdit прав не смотрит.
        => GetFieldLayers(AccessArguments.None, claimId).GetAllFieldsForEdit();
}
