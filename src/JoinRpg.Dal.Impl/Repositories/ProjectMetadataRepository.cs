using JoinRpg.DataModel.Extensions;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.PrimitiveTypes.ProjectMetadata.Payments;
using JoinRpg.PrimitiveTypes.Users;

namespace JoinRpg.Dal.Impl.Repositories;

internal class ProjectMetadataRepository(MyDbContext ctx) : IProjectMetadataRepository
{
    public async Task<ProjectInfo> GetProjectMetadata(ProjectIdentification projectId, bool ignoreCache)
    {
        var project = await ProjectLoaderCommon.GetProjectWithFieldsAsync(ctx, projectId.Value, ignoreCache) ?? throw new InvalidOperationException($"Project with {projectId} not found");

        return CreateInfoFromProject(project, projectId);
    }

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

        return new ProjectInfo(
            projectId,
            new(project.ProjectName),
            project.Details.FieldsOrdering,
            CreateFields(project, fieldSettings).ToList(),
            fieldSettings,
            financeSettings,
            project.Details.EnableAccommodation,
            CharacterIdentification.FromOptional(projectId, project.Details.DefaultTemplateCharacterId),

            //TODO в этих двух полях LazyLoad
            allowToSetGroups: project.CharacterGroups.Any(x => x.IsActive && !x.IsRoot && !x.IsSpecial),
            rootCharacterGroupId: new CharacterGroupIdentification(projectId, project.RootGroup.CharacterGroupId),
            //TODO в этих двух полях LazyLoad

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
            allowManyClaims: project.Details.EnableManyCharacters);

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
                    CharacterGroupIdentification.FromOptional(projectId, field.CharacterGroupId));
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
                            variant.ProgrammaticValue
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
                            variant.ProgrammaticValue
                        );
                }
            }
        }
    }

    public async Task<PrimitiveTypes.ProjectMetadata.ProjectDetails> GetProjectDetails(ProjectIdentification projectId)
    {
        var project = await ProjectLoaderCommon.GetProjectWithFieldsAsync(ctx, projectId.Value, skipCache: false) ?? throw new InvalidOperationException($"Project with {projectId} not found");

        return new PrimitiveTypes.ProjectMetadata.ProjectDetails(
            project.Details.ProjectAnnounce,
            [.. project.KogdaIgraGames.Select(k => new KogdaIgraIdentification(k.KogdaIgraGameId))]);
    }
}
