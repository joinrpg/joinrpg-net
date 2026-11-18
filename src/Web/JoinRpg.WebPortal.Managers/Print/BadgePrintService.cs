using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Common.WebComponents.Badges;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.Data.Interfaces.ProjectMetadata;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.ProjectMetadata.Badges;
using JoinRpg.Web.Models;
using JoinRpg.Web.ProjectMasterTools.Print;

namespace JoinRpg.WebPortal.Managers.Print;

/// <summary>
/// Бейджи персонажей по шаблону: значения полей подставляются в строки шаблона.
/// </summary>
/// <remarks>
/// Печать открывается на весь проект, поэтому всё грузится пачкой: агрегаты одним запросом,
/// пользователи из полей-ссылок — одним словарём.
/// </remarks>
public class BadgePrintService(
    IProjectBadgeTemplateRepository templateRepository,
    IProjectMetadataRepository projectMetadataRepository,
    ICharacterInfoRepository characterInfoRepository,
    IUserRepository userRepository)
{
    /// <returns><c>null</c>, если шаблона нет или он удалён</returns>
    public async Task<BadgesPrintViewModel?> GetBadges(
        ProjectBadgeTemplateIdentification templateId,
        IReadOnlyCollection<CharacterIdentification> characterIds)
    {
        var template = await templateRepository.GetByIdAsync(templateId);
        if (template is not { IsActive: true })
        {
            return null;
        }

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(templateId.ProjectId);
        var fields = projectInfo.UnsortedFields.ToDictionary(f => f.Id.ProjectFieldId);

        // По алфавиту имён: порядок списка до сюда не доходит (CompressedIntList в ссылке сортирует id),
        // а нарезанные бейджи по алфавиту легче раздавать на регистрации.
        var characters = (await characterInfoRepository.GetCharacterInfos(characterIds))
            .Where(c => c.IsActive && !c.CharacterTypeInfo.IsSlot) // шаблонам бейдж не нужен
            .OrderBy(c => c.CharacterName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var values = characters.Select(c => c.GetAllFieldsDict(c.ApprovedClaim?.ClaimId)).ToList();
        var users = await userRepository.LoadFieldUserLinks(values.SelectMany(v => v.Values));

        var badges = values
            .Select(characterValues => (IReadOnlyList<PrintableBadgeLine>)[.. template.Layout.Lines.Select(line =>
                new PrintableBadgeLine(
                    LineText(line, templateId.ProjectId, characterValues, users),
                    line.OffsetMm,
                    line.MinFontPt,
                    line.MaxFontPt,
                    line.Bold,
                    line.Anchor == BadgeLineAnchor.Bottom))])
            .ToList();

        return new BadgesPrintViewModel(
            template.Name,
            template.Layout.WidthMm,
            template.Layout.HeightMm,
            template.Layout.BackgroundUrl,
            badges,
            GetWarnings(template.Layout, fields));
    }

    private static string LineText(
        BadgeLine line,
        ProjectIdentification templateProjectId,
        IReadOnlyDictionary<ProjectFieldIdentification, FieldWithValue> characterValues,
        IReadOnlyDictionary<UserIdentification, UserInfoHeader> users)
    {
        if (line.ProjectFieldId is not int fieldId)
        {
            return line.StaticText ?? "";
        }

        // Агрегат отдаёт и значения удалённых полей, но печатать их нельзя: страница предупреждает,
        // что строка будет пустой, — так мастер увидит проблему в шаблоне, а не старое значение.
        if (characterValues.GetValueOrDefault(new ProjectFieldIdentification(templateProjectId, fieldId)) is not { Field.IsActive: true } value)
        {
            return "";
        }

        if (value.UserIds.Count > 0)
        {
            return string.Join(", ", value.UserIds.Select(id => users.GetValueOrDefault(id)?.DisplayName.DisplayName).OfType<string>());
        }

        return value.DisplayString;
    }

    // TODO[Localize]
    private static List<string> GetWarnings(BadgeLayout layout, IReadOnlyDictionary<int, ProjectFieldInfo> fields)
    {
        var warnings = new List<string>();
        foreach (var fieldId in layout.Lines.Select(l => l.ProjectFieldId).OfType<int>().Distinct())
        {
            if (fields.GetValueOrDefault(fieldId) is not { } field)
            {
                warnings.Add($"В шаблоне есть строка с полем, которого больше нет в проекте (id {fieldId}) — она будет пустой.");
            }
            else if (!field.IsActive)
            {
                warnings.Add($"Поле «{field.Name}» удалено — строка с ним будет пустой. Уберите её из шаблона или восстановите поле.");
            }
        }
        return warnings;
    }
}
