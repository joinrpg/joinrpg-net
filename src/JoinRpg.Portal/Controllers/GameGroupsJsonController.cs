using System.Text.Json;
using JoinRpg.Common.WebComponents;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.Characters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

/// <summary>
/// Публичный JSON с ролями проекта: им пользуются внешние сайты игр, поэтому формат ответа и
/// адреса менять нельзя.
/// </summary>
[Route("{projectId}/roles/{characterGroupId}/[action]")]
public class GameGroupsJsonController(
    IUriService uriService,
    IUriLocator<UserLinkViewModel> userLinkLocator,
    IProjectMetadataRepository projectMetadataRepository,
    ICharacterGroupRepository charGroupRepository,
    ICharacterInfoRepository characterInfoRepository,
    ICurrentUserAccessor currentUserAccessor
    ) : JoinControllerGameBase
{
    [HttpGet]
    [HttpGet("~/{projectId}/roles/hotjson")]
    public async Task<ActionResult> HotJson(CharacterGroupIdentification characterGroupId, int? maxCount = null)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(characterGroupId.ProjectId);
        if (projectInfo.GetGroupByIdOrDefault(characterGroupId) is null)
        {
            return NotFound();
        }

        var characters = await LoadCharactersOfSubtree(characterGroupId, projectInfo);

        var hotRoles = HotCharactersViewModel
            .GetHotCharacters(characters, currentUserAccessor.UserIdentificationOrDefault)
            .Shuffle()
            .Take(maxCount ?? int.MaxValue);

        return ReturnJson(hotRoles.Select(ConvertCharacterToJson));
    }

    [HttpGet("~/{projectId}/roles/{characterGroupId}/indexjson")]
    [AllowAnonymous]
    public async Task<ActionResult> IndexJson(CharacterGroupIdentification characterGroupId)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(characterGroupId.ProjectId);
        if (projectInfo.GetGroupByIdOrDefault(characterGroupId) is not { } rootGroup)
        {
            return NotFound();
        }

        var groupIds = projectInfo.GetChildGroupIdsIncludingThis([characterGroupId]).ToList();
        var characters = await LoadCharactersOfSubtree(characterGroupId, projectInfo);
        // Описания групп в ProjectInfo не входят — грузим их одним запросом на всё поддерево.
        var groupFullInfos = (await charGroupRepository.GetCharacterGroupsFullInfo(groupIds)).ToDictionary(g => g.Id);

        var hasMasterAccess = projectInfo.HasMasterAccess(currentUserAccessor);
        return ReturnJson(new
        {
            ProjectId = characterGroupId.ProjectId.Value,
            projectInfo.ProjectName,
            ShowEditControls = hasMasterAccess,
            Groups = CharacterGroupListViewModel.GetGroups(
                rootGroup,
                characters,
                groupFullInfos,
                currentUserAccessor.UserIdentificationOrDefault,
                projectInfo).Select(
                g =>
                  new
                  {
                      g.CharacterGroupId,
                      g.Name,
                      g.DeepLevel,
                      g.FirstCopy,
                      Description = g.Description?.ToHtmlString(),
                      Path = g.Path.Select(gr => gr.Name),
                      PathIds = g.Path.Select(gr => gr.CharacterGroupId),
                      Characters = g.PublicCharacters.Select(ConvertCharacterToJson),
                      CanAddDirectClaim = false,
                      DirectClaimsCount = (string?)null,
                      DirectClaimLink = (string?)null,
                  }),
        });
    }

    /// <summary>
    /// Персонажи всего поддерева группы доменными агрегатами (ADR013) — по ним считается доступность
    /// заявки и занятость роли.
    /// </summary>
    private async Task<IReadOnlyCollection<CharacterInfo>> LoadCharactersOfSubtree(
        CharacterGroupIdentification rootGroupId,
        ProjectInfo projectInfo)
        => await characterInfoRepository.GetCharacterInfosByGroups(
            rootGroupId.ProjectId,
            [.. projectInfo.GetChildGroupIdsIncludingThis([rootGroupId])]);

    private JsonResult ReturnJson(object data)
    {
        Response.Headers.Append("Access-Control-Allow-Origin", "*");

        return Json(data, new JsonSerializerOptions()
        {
            PropertyNamingPolicy = null, // Pascal case
        });
    }

    private object ConvertCharacterToJson(CharacterViewModel ch)
        => new PublicCharacterJsonBuilder(
                uriService,
                userLinkLocator,
                c => GetFullyQualifiedUri("AddForCharacter", "Claim", new { c.ProjectId, c.CharacterId }))
            .Build(ch);

    private string GetFullyQualifiedUri(
        string actionName,
        string controllerName,
        object routeValues)
    {
        var url = new Uri(Request.GetDisplayUrl());

        return url.Scheme + "://" + url.Host +
               (url.IsDefaultPort ? "" : $":{url.Port}") +
               Url.Action(actionName, controllerName, routeValues);
    }
}
