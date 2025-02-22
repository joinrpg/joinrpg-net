using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Web.Models.Subscribe;

public interface ISubscribeTarget : ILinkable
{
    string Name { get; }
}
