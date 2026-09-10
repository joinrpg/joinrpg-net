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
