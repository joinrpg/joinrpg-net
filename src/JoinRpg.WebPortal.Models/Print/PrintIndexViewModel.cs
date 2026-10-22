namespace JoinRpg.Web.Models.Print;

public record class PrintIndexViewModel(
    int ProjectId,
    IReadOnlyCollection<CharacterIdentification> CharacterIds,
    IReadOnlyCollection<BadgeTemplateLink> BadgeTemplates);

/// <summary>Активный шаблон бейджа — для ссылок на печать</summary>
public record class BadgeTemplateLink(int BadgeTemplateId, string Name);
