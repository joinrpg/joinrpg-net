using JoinRpg.DomainTypes.Forums;
using JoinRpg.DomainTypes.Plots;

namespace JoinRpg.DataModel.Extensions;

/// <summary>
/// Извлечение типизированного идентификатора из EF-сущности.
/// </summary>
/// <remarks>
/// Живёт в <c>JoinRpg.DataModel</c>, а не в <c>JoinRpg.Domain</c>, потому что нужно и слою доступа
/// к данным: <c>JoinRpg.Dal.Impl</c> и <c>JoinRpg.Data.Interfaces</c> перекладывают сущности в DTO,
/// а на <c>JoinRpg.Domain</c> ни тот, ни другой не ссылается и ссылаться не должен (ADR014).
/// Тот же случай, что у <see cref="ClaimExtensions"/>.
///
/// До этого класса те же методы существовали в трёх копиях — в <c>JoinRpg.Domain.IdExtensions</c>,
/// в <c>JoinRpg.Dal.Impl.Repositories.IdExtensions</c> и в <c>PlotIdExtensions</c>. Копии держать
/// нельзя: как только два таких namespace подключены в одном файле, вызов становится неоднозначным.
/// </remarks>
public static class IdExtensions
{
    /// <summary>Идентификатор персонажа.</summary>
    public static CharacterIdentification GetId(this Character character) => new(character.ProjectId, character.CharacterId);

    /// <summary>Идентификатор группы персонажей.</summary>
    public static CharacterGroupIdentification GetId(this CharacterGroup group) => new(group.ProjectId, group.CharacterGroupId);

    /// <summary>Идентификатор заявки.</summary>
    public static ClaimIdentification GetId(this Claim claim) => new(claim.ProjectId, claim.ClaimId);

    /// <summary>Идентификатор папки сюжета.</summary>
    public static PlotFolderIdentification GetId(this PlotFolder folder) => new(folder.ProjectId, folder.PlotFolderId);

    /// <summary>Идентификатор вводной.</summary>
    public static PlotElementIdentification GetId(this PlotElement element)
        => new(element.ProjectId, element.PlotFolderId, element.PlotElementId);

    /// <summary>Идентификатор темы форума.</summary>
    public static ForumThreadIdentification GetId(this ForumThread thread) => new(thread.ProjectId, thread.ForumThreadId);

    /// <summary>Идентификатор поля проекта.</summary>
    public static ProjectFieldIdentification GetId(this ProjectField field) => new(field.ProjectId, field.ProjectFieldId);

    /// <summary>Идентификатор значения поля-справочника.</summary>
    public static ProjectFieldVariantIdentification GetId(this ProjectFieldDropdownValue variant)
        => new(variant.ProjectId, variant.ProjectFieldId, variant.ProjectFieldDropdownValueId);

    /// <summary>Идентификатор одобренной заявки персонажа, если она есть.</summary>
    public static ClaimIdentification? GetApprovedClaimIdOrDefault(this Character character)
        => ClaimIdentification.FromOptional(character.ProjectId, character.ApprovedClaimId);
}
