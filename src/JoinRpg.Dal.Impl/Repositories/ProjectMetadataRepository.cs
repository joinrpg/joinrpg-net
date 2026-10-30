using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.DomainTypes.ProjectMetadata.Accommodation;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.Helpers;

namespace JoinRpg.Dal.Impl.Repositories;

internal class ProjectMetadataRepository(MyDbContext ctx) : IProjectMetadataRepository
{
    public async Task<ProjectInfo> GetProjectMetadata(ProjectIdentification projectId, bool ignoreCache)
    {
        var project = await ProjectLoaderCommon.GetProjectWithFieldsAsync(ctx, projectId.Value, ignoreCache) ?? throw new ProjectNotFoundException(projectId);

        return CreateInfoFromProject(project, projectId);
    }

    // Кэша на этом уровне нет — кэширование добавляет декоратор в web-слое.
    public void PrimeCache(ProjectInfo projectInfo) { }

    // This is internal to allow usage in tests
    internal static ProjectInfo CreateInfoFromProject(Project project, ProjectIdentification projectId)
    {
        var fieldSettings = new ProjectFieldSettings(
            NameField: ProjectFieldIdentification.FromOptional(projectId, project.Details.CharacterNameField?.ProjectFieldId),
            DescriptionField: ProjectFieldIdentification.FromOptional(projectId, project.Details.CharacterDescription?.ProjectFieldId)
            );

        var financeSettings = new ProjectFinanceSettings(
            project.Details.PreferentialFeeEnabled,
            project.Details.FinanceWarnOnOverPayment,
            [..
                project.PaymentTypes.Select(
                    pt => new PaymentTypeInfo(
                        pt.TypeKind,
                        pt.IsActive,
                        pt.User.ToUserInfoHeader(),
                        new PaymentTypeIdentification(pt.ProjectId, pt.PaymentTypeId),
                        pt.Name,
                        pt.IsDefault
                        )
                    )],
            [.. project.ProjectFeeSettings.Select(
                    fee => new ProjectFeeSettingInfo(fee.StartDate, fee.Fee, fee.PreferentialFee, fee.ProjectFeeSettingId))]);

        ProjectLifecycleStatus status = ProjectLoaderCommon.CreateStatus(project.Active, project.IsAcceptingClaims, project.IsBlocked);

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(project.Details.TimeZoneId);

        var groupTree = new ProjectGroupTree(
            project.RootGroup.GetId(),
            CharacterGroupDictionaryBuilder.Build(project, projectId));

        var masters = CreateMasterList(project);
        var masterUsers = masters.ToDictionary(m => m.UserId, m => m.UserInfo);

        return new ProjectInfo(
            projectId,
            new(project.ProjectName),
            project.Details.FieldsOrdering,
            CreateFields(project, fieldSettings).ToList(),
            fieldSettings,
            financeSettings,
            CreateAccommodationSettings(project),
            groupTree: groupTree,
            masters: masters,
            mastersOrdering: project.Details.MastersOrdering,
            publishPlot: project.Details.PublishPlot,
            projectCheckInSettings: new ProjectCheckInSettings(project.Details.CheckInProgress, project.Details.AllowSecondRoles, project.Details.EnableCheckInModule),
            projectStatus: status,
            projectScheduleSettings: new ProjectScheduleSettings(project.Details.ScheduleEnabled),
            projectCloneSettings: project.Details.ProjectCloneSettings,
            createDate: DateOnly.FromDateTime(project.CreatedDate),
            profileRequirementSettings: new ProjectProfileRequirementSettings(
                project.Details.RequireRealName, project.Details.RequireTelegram, project.Details.RequireVkontakte, project.Details.RequirePhone, project.Details.RequirePassport,
                project.Details.RequireRegistrationAddress, project.Details.RequireBirthDate),
            projectClaimSettings: new ProjectClaimSettings(
                DefaultTemplate: CharacterIdentification.FromOptional(projectId, project.Details.DefaultTemplateCharacterId),
                StrictlyOneCharacter: !project.Details.EnableManyCharacters,
                AutoAcceptClaims: project.Details.AutoAcceptClaims,
                IsAcceptingClaims: project.IsAcceptingClaims,
                IsPublicProject: project.Details.IsPublicProject
                ),
            projectRolesLists: CreateRolesLists(project),
            defaultRolesListId: ProjectRolesListIdentification.FromOptional(projectId.Value, project.Details.DefaultProjectRolesListId),
            timeZone: timeZone);

        IReadOnlyCollection<ProjectMasterInfo> CreateMasterList(Project project)
        {
            return [.. project.ProjectAcls.Select(acl => new ProjectMasterInfo(
                new UserIdentification(acl.User.UserId),
                acl.User.ExtractDisplayName(),
                AvatarIdentification.FromOptional(acl.User.SelectedAvatarId),
                new Email(acl.User.Email),
                acl.GetPermissions(),
                acl.IsOwner,
                acl.Status,
                acl.IsPublic,
                acl.ProjectAclId)
                )
                ];
        }

        ProjectAccommodationSettings CreateAccommodationSettings(Project project)
        {
            return new ProjectAccommodationSettings(
                project.Details.EnableAccommodation,
                [.. project.ProjectAccommodationTypes.Select(type => new AccommodationTypeInfo(
                    new AccommodationTypeIdentification(projectId, type.Id),
                    new RoomCategoryIdentification(projectId, type.RoomCategoryId),
                    type.Name,
                    // Описания типа в БД может не быть, а домен ждёт значение — отдаём пустой
                    // markdown, как это сделано для описания персонажа и анонса проекта.
                    new MarkdownString(type.Description?.Contents ?? ""),
                    type.Cost,
                    type.Capacity,
                    type.IsPlayerSelectable))],
                [.. project.ProjectRoomCategories.Select(category => new RoomCategoryInfo(
                    new RoomCategoryIdentification(projectId, category.Id),
                    category.Name))]);
        }

        IEnumerable<ProjectFieldInfo> CreateFields(Project project, ProjectFieldSettings fieldSettings)
        {
            foreach (var field in project.ProjectFields)
            {
                var fieldId = new ProjectFieldIdentification(projectId, field.ProjectFieldId);
                yield return new ProjectFieldInfo(
                    fieldId,
                    field.FieldName,
                    field.FieldType,
                    field.FieldBoundTo,
                    CreateVariants(field, fieldId).ToList(),
                    field.ValuesOrdering,
                    field.Price,
                    field.CanPlayerEdit,
                    field.ShowOnUnApprovedClaims,
                    field.MandatoryStatus,
                    field.ValidForNpc,
                    field.IsActive,
                    [.. CharacterGroupIdentification.FromList(field.AvailableForCharacterGroupIds, projectId)],
                    field.Description,
                    field.MasterDescription,
                    field.IncludeInPrint,
                    fieldSettings,
                        field.ProgrammaticValue,
                        CreateProjectFieldVisibility(field),
                    CharacterGroupIdentification.FromOptional(projectId, field.CharacterGroupId),
                    WasEverUsed: field.WasEverUsed,
                    ProjectTimeZone: timeZone,
                    Marks: new CreateUpdateMarksInfo(
                        field.CreatedAt,
                        masterUsers.GetValueOrDefault(new UserIdentification(field.CreatedById)),
                        field.UpdatedAt,
                        masterUsers.GetValueOrDefault(new UserIdentification(field.UpdatedById))));
            }

            static ProjectFieldVisibility CreateProjectFieldVisibility(ProjectField field)
            {
                return field switch
                {
                    { IsPublic: true, CanPlayerView: true } => ProjectFieldVisibility.Public,
                    { IsPublic: false, CanPlayerView: true } => ProjectFieldVisibility.PlayerAndMaster,
                    { IsPublic: false, CanPlayerView: false } => ProjectFieldVisibility.MasterOnly,
                    { IsPublic: true, CanPlayerView: false } => throw new InvalidOperationException("Invalid combination of flags"),
                };
            }
        }

        IEnumerable<ProjectFieldVariant> CreateVariants(ProjectField field, ProjectFieldIdentification fieldId)
        {
            if (field.FieldType == ProjectFieldType.ScheduleTimeSlotField)
            {
                foreach (var variant in field.DropdownValues)
                {
                    yield return new TimeSlotFieldVariant(
                            new(fieldId, variant.ProjectFieldDropdownValueId),
                            variant.Label,
                            variant.Price,
                            variant.PlayerSelectable,
                            variant.IsActive,
                            CharacterGroupIdentification.FromOptional(projectId, variant.CharacterGroupId),
                            variant.Description,
                            variant.MasterDescription,
                            variant.ProgrammaticValue,
                            wasEverUsed: variant.WasEverUsed,
                            field.FieldName,
                            timeZone
                        );
                }
            }
            else
            {
                foreach (var variant in field.DropdownValues)
                {
                    yield return new ProjectFieldVariant(
                            new(fieldId, variant.ProjectFieldDropdownValueId),
                            variant.Label,
                            variant.Price,
                            variant.PlayerSelectable,
                            variant.IsActive,
                            CharacterGroupIdentification.FromOptional(projectId, variant.CharacterGroupId),
                            variant.Description,
                            variant.MasterDescription,
                            variant.ProgrammaticValue,
                            WasEverUsed: variant.WasEverUsed,
                            field.FieldName
                        );
                }
            }
        }

        IReadOnlyCollection<DomainTypes.ProjectMetadata.ProjectRolesList> CreateRolesLists(Project project)
        {
            var result = new List<DomainTypes.ProjectMetadata.ProjectRolesList>();
            foreach (var entity in project.ProjectRolesLists)
            {
                var fields = entity.FieldIds
                    .Select(fieldId => new ProjectFieldIdentification(projectId, fieldId))
                    .ToList();

                result.Add(new DomainTypes.ProjectMetadata.ProjectRolesList(
                    new ProjectRolesListIdentification(projectId, entity.ProjectRolesListId),
                    entity.Name,
                    CharacterGroupIdentification.FromOptional(projectId, entity.CharacterGroupId),
                    entity.PublicMode,
                    fields,
                    entity.ContactsColumn,
                    entity.GroupsColumn,
                    entity.GroupsViewMode,
                    entity.ShowRolesFilter,
                    entity.PlayerAvatar
                ));
            }
            return result;
        }
    }

    public async Task<DomainTypes.ProjectMetadata.ProjectDetails> GetProjectDetails(ProjectInfo projectInfo)
    {
        // Метаданные уже собраны — догружаем только то, чего в них нет, без графа проекта.
        var projectId = projectInfo.ProjectId;
        var row = await ctx.ProjectsSet
            .Where(p => p.ProjectId == projectId.Value)
            .Select(p => new
            {
                Announce = p.Details.ProjectAnnounce.Contents,
                ClaimApplyRules = p.Details.ClaimApplyRules.Contents,
                p.Details.DisableKogdaIgraMapping,
                PreferentialFeeConditions = p.Details.PreferentialFeeConditions.Contents,
                p.KogdaIgraGames,
            })
            .SingleOrDefaultAsync() ?? throw new ProjectNotFoundException(projectId);

        return new DomainTypes.ProjectMetadata.ProjectDetails(
            projectInfo,
            // Анонса и правил подачи в БД может не быть, а домен ждёт значение — отдаём
            // пустой markdown, как CharacterInfoMapper делает для описания персонажа.
            new MarkdownString(row.Announce ?? ""),
            new MarkdownString(row.ClaimApplyRules ?? ""),
            [.. row.KogdaIgraGames.Select(KogdaIgraRepository.TryConvert).WhereNotNull()],
            row.DisableKogdaIgraMapping,
            new MarkdownString(row.PreferentialFeeConditions ?? ""));
    }
}
