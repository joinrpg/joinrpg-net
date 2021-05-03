using System.Collections.Generic;

namespace JoinRpg.Web.Models.FieldSetup
{
    public class GameFieldListViewModel
    {
        public IEnumerable<GameFieldEditViewModel> Items { get; }

        public FieldNavigationModel Navigation { get; }

        public GameFieldListViewModel(FieldNavigationModel navigation, IEnumerable<GameFieldEditViewModel> fields)
        {
            Navigation = navigation;
            Items = fields;
        }
    }
}
