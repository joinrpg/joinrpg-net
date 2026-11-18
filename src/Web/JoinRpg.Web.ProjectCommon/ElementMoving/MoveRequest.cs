using System.Diagnostics.CodeAnalysis;
using JoinRpg.DomainTypes.Interfaces;

namespace JoinRpg.Web.ProjectCommon.ElementMoving;

public record MoveRequest(IProjectEntityId SelfId, IProjectEntityId ParentId, IProjectEntityId? MoveAfterId)
{
    public static bool TryParse(
        string selfId,
        string parentId,
        string? moveAfterId,
        [MaybeNullWhen(false)] out MoveRequest result)
    {
        if (!ProjectEntityIdParser.TryParseId(selfId, out var self)
            || !ProjectEntityIdParser.TryParseId(parentId, out var parent))
        {
            result = null;
            return false;
        }


        IProjectEntityId? moveAfter = null;
        if (moveAfterId is not null)
        {
            if (!ProjectEntityIdParser.TryParseId(moveAfterId, out moveAfter))
            {
                result = null;
                return false;
            }

            if (moveAfter.GetType() != self.GetType())
            {
                result = null;
                return false;
            }
        }

        result = new MoveRequest(self, parent, moveAfter);
        return true;
    }
}
