using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Web.Models.CommonTypes;

namespace JoinRpg.Web.Models.FieldSetup
{

    public static class GameFieldViewModelsExtensions
    {
        public static IEnumerable<GameFieldEditViewModel> ToViewModels(this IEnumerable<ProjectField> gameFields, int currentUserId) => gameFields.Select(pf => new GameFieldEditViewModel(pf, currentUserId)).MarkFirstAndLast();
    }
}
