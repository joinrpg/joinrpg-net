using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models;

namespace JoinRpg.Web.Helpers;


public static class ObjectLinkHelper
{
    public static IEnumerable<GameObjectLinkViewModel> AsObjectLinks(
        this IEnumerable<ILinkableWithName> objects,
        IUriService uriService)
    {
        ArgumentNullException.ThrowIfNull(objects);

        return objects.Select(t => new GameObjectLinkViewModel(uriService, t));
    }
}
