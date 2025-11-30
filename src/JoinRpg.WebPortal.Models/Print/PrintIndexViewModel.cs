using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Web.Models.Print;

public record class PrintIndexViewModel(int ProjectId, IReadOnlyCollection<CharacterIdentification> CharacterIds);
