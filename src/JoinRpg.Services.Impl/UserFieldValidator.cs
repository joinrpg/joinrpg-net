using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Services.Impl;

/// <summary>
/// Проверяет, что поля-ссылки на пользователя (ADR017) указывают на существующих пользователей.
/// </summary>
/// <remarks>
/// <para>
/// Живёт отдельно от сохранения полей, потому что <c>FieldSaveHelper</c> синхронный и лежит в
/// <c>JoinRpg.Domain</c> без доступа к репозиториям (ADR017 §7). Поэтому проверка вызывается
/// раньше — в сервисах, принимающих <see cref="FieldLayerContainer"/>.
/// </para>
/// <para>
/// Проверяется ровно факт существования пользователя. Ни заявки, ни доступы упомянутого
/// пользователя не читаются и не меняются: упоминание в поле не даёт никаких прав — ни ему,
/// ни тому, кто его вписал (ADR017 §6).
/// </para>
/// </remarks>
internal class UserFieldValidator(IUserRepository userRepository)
{
    /// <summary>
    /// Один проход по слою и один запрос в базу на все найденные идентификаторы.
    /// Пустое значение поля (снятие ссылки) ошибкой не является.
    /// </summary>
    /// <exception cref="FieldUserNotFoundException">Пользователь не найден.</exception>
    public async Task ValidateUserFields(FieldLayerContainer fieldsToSet)
    {
        List<(ProjectFieldInfo Field, UserIdentification UserId)> mentions = [];
        foreach (var fieldWithValue in fieldsToSet.LayerData.Values)
        {
            foreach (var userId in fieldWithValue.UserIds)
            {
                mentions.Add((fieldWithValue.Field, userId));
            }
        }

        if (mentions.Count == 0)
        {
            // Ссылок на пользователей во входящем слое нет — в базу не ходим.
            return;
        }

        IReadOnlyCollection<UserIdentification> userIds = [.. mentions.Select(m => m.UserId).Distinct()];
        var existing = (await userRepository.GetUserInfoHeaders(userIds)).Select(u => u.UserId).ToHashSet();

        foreach (var (field, userId) in mentions)
        {
            if (!existing.Contains(userId))
            {
                throw new FieldUserNotFoundException(field.Id, field.Name, userId);
            }
        }
    }
}
