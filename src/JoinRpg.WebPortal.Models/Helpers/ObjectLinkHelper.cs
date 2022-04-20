using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models;

namespace JoinRpg.Web.Helpers
{


    public static class ObjectLinkHelper
    {
        public static IEnumerable<GameObjectLinkViewModel> AsObjectLinks(
            [NotNull][ItemNotNull] this IEnumerable<IWorldObject> objects,
            IUriService uriService)
        {
            if (objects == null)
            {
                throw new ArgumentNullException(nameof(objects));
            }

            return objects.Select(t => new GameObjectLinkViewModel(uriService, t));
        }
    }
}
