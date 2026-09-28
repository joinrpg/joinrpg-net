namespace JoinRpg.Web.ProjectCommon;

/// <summary>
/// Элемент списка заявок для <see cref="ClaimSelector"/>
/// </summary>
public record ClaimSelectorItem(int ClaimId, string Name, string Status, string PlayerName);
