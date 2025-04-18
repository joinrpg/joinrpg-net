using System.Data.Entity.Validation;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Extensions;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.Plots;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.Projects;
using Microsoft.Extensions.Logging;

namespace JoinRpg.Services.Impl.Projects;

internal class CloneProjectHelper(
    CloneProjectRequest cloneRequest,
    Project project,
    ProjectIdentification projectId,
    ProjectInfo original,
    Project originalEntity,
    IProjectService projectService,
    IFieldSetupService fieldSetupService,
    IProjectMetadataRepository projectMetadataRepository,
    ICharacterService characterService,
    IPlotService plotService,
    ILogger logger)
{
    private readonly Dictionary<CharacterIdentification, CharacterIdentification> CharacterMapping = [];
    private List<CharacterIdentification> TryMapOriginalCharacterIds(IEnumerable<int> enumerable) => [.. CharacterIdentification.FromList(enumerable, original.ProjectId).Select(id => CharacterMapping.GetValueOrDefault(id)).WhereNotNull()];

    private readonly Dictionary<CharacterGroupIdentification, CharacterGroupIdentification> GroupMapping = [];
    private readonly Dictionary<CharacterGroupIdentification, CharacterGroupIdentification> SpecialGroupMapping = [];
    private List<CharacterGroupIdentification> TryMapGroups(IEnumerable<CharacterGroupIdentification> originalShowForGroups) => [.. originalShowForGroups.Select(g => GroupMapping.GetValueOrDefault(g)).WhereNotNull()];
    private List<CharacterGroupIdentification> TryMapOriginalGroupIds(IEnumerable<int> intList) => TryMapGroups(CharacterGroupIdentification.FromList(intList, original.ProjectId));

    private readonly Dictionary<ProjectFieldIdentification, ProjectFieldIdentification> FieldMapping = [];

    private readonly Dictionary<ProjectFieldVariantIdentification, ProjectFieldVariantIdentification> VariantMapping = [];

    private bool everythingFine = true;
    public async Task<bool> Clone()
    {
        await CopyFields();

        if (cloneRequest.CopySettings >= ProjectCopySettingsDto.SettingsFieldsGroupsAndTemplates)
        {
            await CopyGroups();
            await FixupConditionalFields();

            if (cloneRequest.CopySettings == ProjectCopySettingsDto.SettingsFieldsGroupsAndTemplates)
            {
                await CopyTemplates();
            }
            else
            {
                await CopyCharacters();
            }
        }

        if (cloneRequest.CopySettings >= ProjectCopySettingsDto.SettingsFieldsGroupsCharactersAndPlot)
        {
            await CopyPlot();
        }

        await projectService.EditProject(
            new EditProjectRequest()
            {
                AutoAcceptClaims = originalEntity.Details.AutoAcceptClaims,
                ClaimApplyRules = originalEntity.Details.ClaimApplyRules?.Contents ?? "",
                IsAcceptingClaims = false,
                IsAccommodationEnabled = original.AccomodationEnabled,
                MultipleCharacters = originalEntity.Details.EnableManyCharacters,
                ProjectAnnounce = originalEntity.Details.ProjectAnnounce?.Contents ?? "",
                ProjectId = projectId,
                ProjectName = cloneRequest.ProjectName,
                PublishPlot = false,
                DefaultTemplateCharacterId = original.DefaultTemplateCharacter is not null ? CharacterMapping.GetValueOrDefault(original.DefaultTemplateCharacter) : null,
                // Если у проекта был шаблон по умолчанию, и мы его скопировали — указываем его.
            });

        return everythingFine;
    }

    private async Task CopyGroups()
    {
        var originalList = originalEntity.CharacterGroups
            .Where(cg => cg.IsActive && !cg.IsSpecial)
            .Select(cg => (group: cg, id: new CharacterGroupIdentification(new(originalEntity.ProjectId), cg.CharacterGroupId)))
            .ToList();

        var rootCharacterGroupId = new CharacterGroupIdentification(projectId, project.RootGroup.CharacterGroupId);
        // Первый проход — создаем группы
        foreach ((var originalGroup, var originalId) in originalList)
        {
            var groupId =
                await projectService.AddCharacterGroup(
                    projectId,
                    originalGroup.CharacterGroupName,
                    originalGroup.IsPublic,
                    [rootCharacterGroupId], // Пока помещаем в корень
                    originalGroup.Description.Contents ?? "");
            GroupMapping.Add(originalId, groupId);
        }

        // Второй проход. Все группы уже созданы, указываем для них родителей.
        foreach ((var originalGroup, var originalId) in originalList)
        {
            var targetId = GroupMapping[originalId];
            List<CharacterGroupIdentification> parentGroupIds = TryMapOriginalGroupIds(originalGroup.ParentCharacterGroupIds);
            if (parentGroupIds.Count > 0)
            {
                await projectService.EditCharacterGroup(
                targetId,
                originalGroup.CharacterGroupName,
                originalGroup.IsPublic,
                parentGroupIds,
                originalGroup.Description.Contents ?? "");
            }
            else
            {
                // Не смогли понять, где группа должна находится. Бросаем ее в корне
            }

        }
    }

    private async Task CopyFields()
    {
        ProjectFieldIdentification? description = null;
        ProjectFieldIdentification? name = null;
        foreach (var field in original.SortedActiveFields)
        {
            var newId = await fieldSetupService.AddField(
                        new CreateFieldRequest(
                            projectId,
                            field.Type,
                            field.Name,
                            field.Description?.Contents ?? "",
                            field.CanPlayerEdit,
                            field.CanPlayerView,
                            field.IsPublic,
                            field.BoundTo,
                            field.MandatoryStatus,
                            [], // Будут скопированы позже, если группы копируются
                            field.ValidForNpc,
                            field.IncludeInPrint,
                            field.ShowOnUnApprovedClaims,
                            field.Price,
                            field.MasterDescription?.Contents ?? "",
                            field.ProgrammaticValue)
                        );

            FieldMapping.Add(field.Id, newId);
            if (field.HasValueList)
            {
                foreach (var variant in field.Variants.Where(v => v.IsActive))
                {
                    var newVariantId = await fieldSetupService.CreateFieldValueVariant(new CreateFieldValueVariantRequest(newId, variant.Label, variant.Description?.Contents, variant.MasterDescription?.Contents, variant.ProgrammaticValue, variant.Price, variant.IsPlayerSelectable, (variant as TimeSlotFieldVariant)?.TimeSlotOptions));
                    VariantMapping.Add(variant.Id, newVariantId);
                }
            }

            if (field.IsDescription)
            {
                description = newId;
            }

            if (field.IsName)
            {
                name = newId;
            }
        }

        await fieldSetupService.SetFieldSettingsAsync(new FieldSettingsRequest() { ProjectId = projectId, DescriptionField = description, NameField = name });

        var info = await projectMetadataRepository.GetProjectMetadata(projectId, ignoreCache: true);

        // При создании полей создались спецгруппы. Загрузим и сохраним их меппинг
        foreach (var originalField in original.SortedActiveFields)
        {
            var newField = info.GetFieldById(FieldMapping[originalField.Id]);
            if (originalField.SpecialGroupId is CharacterGroupIdentification originalGroupId && newField.SpecialGroupId is CharacterGroupIdentification newGroupId)
            {
                GroupMapping.Add(originalGroupId, newGroupId);
                SpecialGroupMapping.Add(originalGroupId, newGroupId);
            }
            if (originalField.HasValueList)
            {
                foreach (var originalVariant in originalField.Variants.Where(v => v.IsActive))
                {
                    ProjectFieldVariantIdentification newVariantId = VariantMapping[originalVariant.Id];
                    var newVariant = newField.Variants.Single(v => v.Id == newVariantId);
                    if (originalVariant.CharacterGroupId is CharacterGroupIdentification originalGroupId2
                        && newVariant.CharacterGroupId is CharacterGroupIdentification newGroupId2)
                    {
                        GroupMapping.Add(originalGroupId2, newGroupId2);
                        SpecialGroupMapping.Add(originalGroupId2, newGroupId2);
                    }
                }
            }
        }
    }

    private async Task FixupConditionalFields()
    {
        var info = await projectMetadataRepository.GetProjectMetadata(projectId, ignoreCache: true);

        foreach (var originalField in original.SortedActiveFields)
        {
            var originalShowForGroups = originalField.GroupsAvailableForIds;
            if (originalShowForGroups.Count == 0)
            {
                continue;
            }
            var newField = info.GetFieldById(FieldMapping[originalField.Id]);

            var request = new UpdateFieldRequest(
                newField.Id,
                newField.Name,
                newField.Description.Contents ?? "",
                newField.CanPlayerEdit,
                newField.CanPlayerView,
                newField.IsPublic,
                newField.MandatoryStatus,
                TryMapGroups(originalShowForGroups),
                newField.ValidForNpc,
                newField.IncludeInPrint,
                newField.ShowOnUnApprovedClaims,
                newField.Price,
                newField.MasterDescription.Contents ?? "",
                newField.ProgrammaticValue ?? ""
                );
            await fieldSetupService.UpdateFieldParams(request);
        }

        // Нам нужно загрузить здесь все повторно, иначе кэш метаданных будет видеть дальше условные поля как безусловные, и это может привести к проблемам.
        _ = await projectMetadataRepository.GetProjectMetadata(projectId, ignoreCache: true);
    }

    private async Task CopyCharacters()
    {
        foreach (var originalChar in originalEntity.Characters.Where(c => c.IsActive))
        {
            await CopyCharacter(originalChar);
        }
    }

    private async Task CopyTemplates()
    {
        foreach (var originalChar in originalEntity.Characters.Where(c => c.IsActive && c.CharacterType == CharacterType.Slot))
        {
            await CopyCharacter(originalChar);
        }
    }

    private async Task CopyCharacter(Character originalChar)
    {
        var oldCharacterId = new CharacterIdentification(original.ProjectId, originalChar.CharacterId);
        try
        {
            var fieldValues = originalChar.GetFields(original);
            var setFieldsRequest = new Dictionary<int, string?>();
            foreach (var originalFieldValue in fieldValues
                .Where(fv => fv.Field.BoundTo == FieldBoundTo.Character) // Только поля персонажей
                .Where(fv => fv.HasEditableValue) // Только те поля, у которых есть какое-то значение
                )
            {
                var newFieldId = FieldMapping.GetValueOrDefault(originalFieldValue.Field.Id);
                if (newFieldId is null)
                {
                    continue; // Это поле было удалено, следовательно его значение мы не переносим.
                }

                var value = MapFieldValue(originalFieldValue);
                setFieldsRequest.Add(newFieldId.ProjectFieldId, value);
            }
            List<CharacterGroupIdentification> parentCharacterGroupIds = [..
                TryMapOriginalGroupIds(originalChar.ParentCharacterGroupIds)
                .Except(SpecialGroupMapping.Values) // Без спецгрупп, они автоматически проставятся по значениям полей
                ];
            var request = new AddCharacterRequest(projectId, parentCharacterGroupIds, originalChar.ToCharacterTypeInfo(), setFieldsRequest);
            var newId = await characterService.AddCharacter(request);

            CharacterMapping.Add(oldCharacterId, newId);
        }
        catch (DbEntityValidationException ex)
        {
            logger.LogWarning(ex, "Не удалось скопировать персонажа {characterId} в проект {projectId}. Копирование проекта будет продолжено.", oldCharacterId, projectId);
            everythingFine = false;
        }
        catch (JoinRpgBaseException ex)
        {
            logger.LogWarning(ex, "Не удалось скопировать персонажа {characterId} в проект {projectId}. Копирование проекта будет продолжено.", oldCharacterId, projectId);
            everythingFine = false;
        }

    }

    private string? MapFieldValue(FieldWithValue originalFieldValue)
    {
        if (!originalFieldValue.Field.HasValueList)
        {
            return originalFieldValue.Value;
        }

        // Нам нужно смеппить старые значения на новые
        List<ProjectFieldVariantIdentification> values = [];
        foreach (var originalValue in originalFieldValue.GetDropdownValues())
        {
            var newFieldValue = VariantMapping.GetValueOrDefault(originalValue.Id);
            if (newFieldValue is not null) // Если не нашли, значит этот вариант удалили, следовательно мы его не переносим
            {
                values.Add(newFieldValue);
            }
        }

        return string.Join(",", values.Select(x => x.ProjectFieldVariantId.ToString()));
    }

    private async Task CopyPlot()
    {
        foreach (var originalFolder in originalEntity.PlotFolders.Where(c => c.IsActive))
        {
            var originalPlotId = new PlotFolderIdentification(original.ProjectId, originalFolder.PlotFolderId);
            try
            {
                var plotFolderId = await plotService.CreatePlotFolder(projectId, originalFolder.MasterTitle, originalFolder.TodoField);

                foreach (var originalElement in originalFolder.Elements.Where(e => e.IsActive))
                {
                    PlotElementTexts lastVersion = originalElement.LastVersion();
                    _ = await plotService.CreatePlotElement(
                        plotFolderId,
                        lastVersion.Content.Contents ?? "",
                        lastVersion.TodoField,
                        TryMapOriginalGroupIds(originalElement.TargetGroups.Select(g => g.CharacterGroupId)),
                        TryMapOriginalCharacterIds(originalElement.TargetCharacters.Select(c => c.CharacterId)),
                        originalElement.ElementType);
                }
            }
            catch (DbEntityValidationException ex)
            {
                logger.LogWarning(ex, "Не удалось скопировать сюжет {originalPlotId} в проект {projectId}. Копирование проекта будет продолжено.", originalPlotId, projectId);
                everythingFine = false;
            }
            catch (JoinRpgBaseException ex)
            {
                logger.LogWarning(ex, "Не удалось скопировать сюжет {originalPlotId} в проект {projectId}. Копирование проекта будет продолжено.", originalPlotId, projectId);
                everythingFine = false;
            }
        }
    }


}
