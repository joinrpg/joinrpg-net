using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.ProjectMetadata.Badges;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.ProjectMasterTools.Badges;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JoinRpg.Portal.Pages.GamePages;

[RequireMaster(Permission.CanChangeProjectProperties)]
[ProjectShouldBeActive] // как и сервис шаблонов (MustBeActive): в заблокированном проекте сохранить нельзя
public class BadgeTemplateEditModel(IBadgeTemplateClient client, ICharacterInfoRepository characterInfoRepository) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public required ProjectIdentification ProjectId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? BadgeTemplateId { get; set; }

    public ProjectBadgeTemplateIdentification? TemplateId
        => BadgeTemplateId is int id ? new ProjectBadgeTemplateIdentification(ProjectId, id) : null;

    public EditBadgeTemplateViewModel? Model { get; private set; }

    /// <summary>Активные персонажи — для ссылок «напечатать всех этим шаблоном». Пусто у нового шаблона</summary>
    public IReadOnlyCollection<CharacterIdentification> ActiveCharacterIds { get; private set; } = [];

    public async Task<IActionResult> OnGet()
    {
        if (TemplateId is { } templateId)
        {
            try
            {
                Model = await client.GetById(templateId);
                // Лёгкая проекция, а не агрегаты: странице нужны только идентификаторы для ссылок.
                ActiveCharacterIds = [.. (await characterInfoRepository.GetCharactersForList(ProjectId, CharacterStatusSpec.Active)).Select(c => c.Id)];
            }
            catch (JoinRpgEntityNotFoundException)
            {
                return NotFound();
            }
        }
        else
        {
            Model = EditBadgeTemplateViewModel.CreateNew();
        }
        return Page();
    }
}
