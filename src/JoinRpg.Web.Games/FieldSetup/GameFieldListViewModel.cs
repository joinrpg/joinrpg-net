namespace JoinRpg.Web.Games.FieldSetup;

public record GameFieldListViewModel(FieldNavigationModel Navigation, IEnumerable<GameFieldListItemViewModel> Items);
