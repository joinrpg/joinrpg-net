using JoinRpg.Services.Impl.Claims;

namespace JoinRpg.Services.Impl.Test.Fakes;

/// <summary>
/// Записывает утверждение заявки вместо реального. Нужен, чтобы проверить <b>когда</b> автоприём
/// зовут: настоящее утверждение — самостоятельная операция со своими правами и сохранением, и
/// подменять её здесь нечем (подмена пользователя в фейках не моделируется).
/// </summary>
internal sealed class FakeClaimApprovalService : IClaimApprovalService
{
    /// <summary>Вызовы в порядке поступления.</summary>
    public List<(ClaimIdentification ClaimId, string CommentText)> Calls { get; } = [];

    /// <summary>Выполняется в момент вызова — даёт заглянуть в состояние мира изнутри.</summary>
    public Action? OnApprove { get; set; }

    public Task ApproveByMaster(ClaimIdentification claimId, string commentText)
    {
        Calls.Add((claimId, commentText));
        OnApprove?.Invoke();
        return Task.CompletedTask;
    }
}
