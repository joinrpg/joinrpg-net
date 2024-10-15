namespace JoinRpg.Web.ProjectCommon;

public interface ICharactersClient
{
    Task<List<CharacterDto>> GetCharacters(int projectId);
    Task<List<CharacterDto>> GetTemplateCharacters(int projectId);
}
