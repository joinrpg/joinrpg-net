using JoinRpg.DomainTypes.Forums;

namespace JoinRpg.Services.Interfaces;

public interface IForumService
{
    Task<ForumThreadIdentification> CreateThread(CharacterGroupIdentification characterGroupId, string header, string commentText, bool hideFromUser, bool emailEverybody);
}
