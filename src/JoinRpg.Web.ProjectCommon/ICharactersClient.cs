namespace JoinRpg.Web.ProjectCommon;

public interface ICharactersClient
{
    Task<List<CharacterDto>> GetCharacters(ProjectIdentification projectId, CharacterListType listType = CharacterListType.All);
}

public enum CharacterListType
{
    All,
    AllTemplates,
    Available,
    AvailableNonSlots,
}

