namespace JoinRpg.Web.Models.Print;

public record class PrintIndexViewModel(
    int ProjectId,
    IReadOnlyCollection<CharacterIdentification> CharacterIds,
    IReadOnlyCollection<BadgeTemplateLink> BadgeTemplates);

/// <summary>Активный шаблон бейджа — для ссылок на печать</summary>
public record class BadgeTemplateLink(int BadgeTemplateId, string Name);

/// <summary>Выбор шаблона для печати бейджей выбранных персонажей</summary>
/// <param name="CharacterIds">Персонажи в сжатом виде — как пришли в запросе, передаются дальше в ссылки</param>
public record class BadgeTemplateChoiceViewModel(
    int ProjectId,
    string CharacterIds,
    IReadOnlyCollection<BadgeTemplateLink> Templates);
