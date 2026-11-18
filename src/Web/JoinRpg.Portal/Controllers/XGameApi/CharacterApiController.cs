using System.Text.Json;
using JoinRpg.Domain;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.WebPortal.Managers.Characters;
using JoinRpg.XGameApi.Contract;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.XGameApi;

[Route("x-game-api/{projectId}/characters"), XGameMasterAuthorize()]
public class CharacterApiController(ICharacterApiViewService characterApiViewService) : XGameApiController()
{

    /// <summary>
    /// Load character list. If you aggressively pull characters,
    /// please use modifiedSince parameter.
    /// </summary>
    [HttpGet]
    [Route("")]
    public async Task<IEnumerable<CharacterHeader>> GetList(int projectId,
        [FromQuery]
        DateTime? modifiedSince = null)
    {
        return await characterApiViewService.GetCharacterHeaders(new ProjectIdentification(projectId), modifiedSince);
    }

    /// <summary>
    /// Character details
    /// </summary>
    [HttpGet]
    [Route("{characterId}/")]
    public async Task<CharacterInfo> GetOne(int projectId, int characterId)
    {
        return await characterApiViewService.GetCharacterInfo(new CharacterIdentification(new ProjectIdentification(projectId), characterId));
    }

    /// <summary>
    /// Character details for several characters at once. Use when you need a specific
    /// set of characters, not the full project list.
    /// </summary>
    [HttpGet]
    [Route("by-ids")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<CharacterInfo>>> GetByIds(int projectId, [FromQuery] int[] ids)
    {
        if (ids is not { Length: > 0 })
        {
            return BadRequest("ids is required");
        }

        return Ok(await characterApiViewService.GetCharactersByIds(new ProjectIdentification(projectId), ids));
    }

    /// <summary>
    /// Characters belonging to a group, including nested subgroups
    /// (and field-variant special groups).
    /// </summary>
    [HttpGet]
    [Route("~/x-game-api/{projectId}/groups/{groupId}/characters")]
    public async Task<IEnumerable<CharacterInfo>> GetByGroup(int projectId, int groupId)
    {
        return await characterApiViewService.ListCharactersByGroup(
            new CharacterGroupIdentification(new ProjectIdentification(projectId), groupId));
    }

    /// <summary>
    /// Create new character
    /// </summary>
    [HttpPost]
    [Route("")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesDefaultResponseType]
    public async Task<ActionResult<CharacterHeader>> CreateCharacter(int projectId, [FromBody] CreateCharacterRequest request)
    {
        CharacterHeader character;
        try
        {
            character = await characterApiViewService.CreateCharacter(new ProjectIdentification(projectId), request);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            // Поля, которого нет в проекте, раньше хватало на 500 из глубины домена.
            return BadRequest(ex.Message);
        }

        return CreatedAtAction(nameof(GetOne), new { projectId, characterId = character.CharacterId }, character);
    }

    /// <summary>
    /// Allows to set character fields as master
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="characterId">Character ID</param>
    /// <param name="fieldValues">
    /// Key = FieldId, Value = field value (for Select/Multiselect - id of value)
    /// Skipped values will be left unchanged</param>
    [HttpPost]
    [Route("{characterId}/fields")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesDefaultResponseType]
    public async Task<ActionResult<string>> SetCharacterFields(int projectId, int characterId, [FromBody] Dictionary<int, JsonElement> fieldValues)
    {
        try
        {
            await characterApiViewService.SetCharacterFields(new CharacterIdentification(projectId, characterId), fieldValues);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            // Поля, которого нет в проекте, раньше хватало на 500 из глубины домена.
            return BadRequest(ex.Message);
        }
        catch (FieldCannotHaveValueException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (FieldValueInvalidException ex)
        {
            return BadRequest(ex.Message);
        }
        return "ok";
    }

}
