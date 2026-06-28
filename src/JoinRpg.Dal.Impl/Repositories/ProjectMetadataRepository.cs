using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.DataModel.Extensions;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;

namespace JoinRpg.Dal.Impl.Repositories;

internal class ProjectMetadataRepository(MyDbContext ctx) : IProjectMetadataRepository
{
    public async Task<ProjectInfo> GetProjectMetadata(ProjectIdentification projectId, bool ignoreCache)
    {
        var project = await ProjectLoaderCommon.GetProjectWithFieldsAsync(ctx, projectId.Value, ignoreCache) ?? throw new InvalidOperationException($"Project with {projectId} not found");

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
            [..
                project.PaymentTypes.Select(
                    pt => new PaymentTypeInfo(
                        pt.TypeKind,
                        pt.IsActive,
                        new UserInfoHeader(new UserIdentification(pt.User.UserId), pt.User.ExtractDisplayName()),
                        new PaymentTypeIdentification(pt.ProjectId, pt.PaymentTypeId)
                        )
                    )]);

        ProjectLifecycleStatus status = ProjectLoaderCommon.CreateStatus(project.Active, project.IsAcceptingClaims);

        var groups = CharacterGroupDictionaryBuilder.Build(project, projectId);

        return new ProjectInfo(
            projectId,
            new(project.ProjectName),
            project.Details.FieldsOrdering,
            CreateFields(project, fieldSettings).ToList(),
            fieldSettings,
            financeSettings,
            project.Details.EnableAccommodation,
            allowToSetGroups: project.CharacterGroups.Any(x => x.IsActive && !x.IsRoot && !x.IsSpecial),
            rootCharacterGroupId: project.RootGroup.GetId(),
            masters: CreateMasterList(project),
            publishPlot: project.Details.PublishPlot,
            projectCheckInSettings: new ProjectCheckInSettings(project.Details.EnableCheckInModule, project.Details.CheckInProgress, project.Details.AllowSecondRoles),
            projectStatus: status,
            projectScheduleSettings: new ProjectScheduleSettings(project.Details.ScheduleEnabled),
            projectCloneSettings: project.Details.ProjectCloneSettings,
            createDate: DateOnly.FromDateTime(project.CreatedDate),
            profileRequirementSettings: new ProjectProfileRequirementSettings(
                project.Details.RequireRealName, project.Details.RequireTelegram, project.Details.RequireVkontakte, project.Details.RequirePhone, project.Details.RequirePassport,
                project.Details.RequireRegistrationAddress),
            projectClaimSettings: new ProjectClaimSettings(
                DefaultTemplate: CharacterIdentification.FromOptional(projectId, project.Details.DefaultTemplateCharacterId),
                StrictlyOneCharacter: !project.Details.EnableManyCharacters,
                AutoAcceptClaims: project.Details.AutoAcceptClaims,
                IsAcceptingClaims: project.IsAcceptingClaims,
                IsPublicProject: project.Details.IsPublicProject
                ),
            projectRolesLists: CreateRolesLists(project),
            groups: groups);

        IReadOnlyCollection<ProjectMasterInfo> CreateMasterList(Project project)
        {
            return [.. project.ProjectAcls.Select(acl => new ProjectMasterInfo(
                new UserIdentification(acl.User.UserId),
                acl.User.ExtractDisplayName(),
                new Email(acl.User.Email),
                acl.GetPermissions())
                )
                ];
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
                    WasEverUsed: field.WasEverUsed);
            }

            static ProjectFieldVisibility CreateProjectFieldVisibility(ProjectField field)
            {
                return field switch
                {
                    { IsPublic: true, CanPlayerView: true } => ProjectFieldVisibility.Public,
                    { IsPublic: false, CanPlayerView: true } => ProjectFieldVisibility.PlayerAndMaster,
                    { IsPublic: false, CanPlayerView: false } => ProjectFieldVisibility.MasterOnly,
                    { IsPublic: true, CanPlayerView: false } => throw new InvalidOperationException("Invalid combination of flagss"),
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
                            wasEverUsed: variant.WasEverUsed
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
                            WasEverUsed: variant.WasEverUsed
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
                    entity.ShowCharacterGroups
                ));
            }
            return result;
        }
    }

    public async Task<DomainTypes.ProjectMetadata.ProjectDetails> GetProjectDetails(ProjectIdentification projectId)
    {
        var project = await ProjectLoaderCommon.GetProjectWithFieldsAsync(ctx, projectId.Value, skipCache: false) ?? throw new InvalidOperationException($"Project with {projectId} not found");

        return new DomainTypes.ProjectMetadata.ProjectDetails(
            project.Details.ProjectAnnounce,
            [.. project.KogdaIgraGames.Where(x => x.Active).Select(k => new KogdaIgraIdentification(k.KogdaIgraGameId))],
            project.Details.DisableKogdaIgraMapping);
    }
}
