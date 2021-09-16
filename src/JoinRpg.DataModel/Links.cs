using JetBrains.Annotations;

namespace JoinRpg.DataModel
{
    // TODO add unit test to ensure that everything covered
    public enum LinkType
    {
        ResultUser,
        ResultCharacterGroup,
        ResultCharacter,
        Claim,
        Plot,
        Comment,
        Project,
        CommentDiscussion,
        PaymentSuccess,
        PaymentFail,
    }

    public interface ILinkable
    {
        LinkType LinkType { get; }
        [NotNull]
        string Identification { get; }
        int? ProjectId { get; }
    }
}
