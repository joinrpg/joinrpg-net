namespace JoinRpg.Portal.Infrastructure.ModelBinding;

/// <summary>
/// Разрешает биндить параметр-идентификатор (<see cref="JoinRpg.DomainTypes.Interfaces.IProjectEntityId"/>)
/// значением из другого проекта, а не только из текущего (<see cref="JoinRpg.WebPortal.Managers.Interfaces.ICurrentProjectAccessor"/>).
/// Работает только когда идентификатор передан полной строкой (например "Character(5-1)") —
/// если передано просто число, биндинг с этим атрибутом всегда завершается ошибкой,
/// так как несовпадающий проект нельзя выразить одним числом.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class AllowAnotherProjectAttribute : Attribute;
