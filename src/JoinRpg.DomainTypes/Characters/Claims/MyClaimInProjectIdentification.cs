using System.Text.Json.Serialization;
using JoinRpg.DomainTypes.Interfaces;

namespace JoinRpg.DomainTypes;

/// <summary>
/// Заявка получателя уведомления в проекте <see cref="Value"/> — без номера: какая именно, решает
/// страница «моей заявки» при переходе. Нужен там, где одно уведомление уходит многим и у каждого
/// должна быть ссылка на свою заявку, например в массовой рассылке (#5752).
/// </summary>
[method: JsonConstructor]
[TypedEntityId]
public partial record MyClaimInProjectIdentification(int Value) : IProjectEntityId
{
    public MyClaimInProjectIdentification(ProjectIdentification projectId) : this(projectId.Value) { }

    public ProjectIdentification ProjectId => new(Value);
}
