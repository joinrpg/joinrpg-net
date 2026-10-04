using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Common.WebComponents;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Web.Models;

/// <summary>
/// Подгружает пользователей, на которых ссылаются поля типа UserLink (ADR017).
/// </summary>
/// <remarks>
/// Всегда один запрос на экран: страницу персонажа/заявки и пачку печати рисуем по одному
/// словарю. Запрос в цикле по полям ловится интеграционными тестами ленивых загрузок.
/// </remarks>
public static class FieldUserLinksLoader
{
    /// <summary>
    /// Пустой словарь — когда ссылок на пользователей на экране заведомо нет
    /// (например, у только что созданного персонажа значений полей ещё не существует).
    /// </summary>
    public static IReadOnlyDictionary<UserIdentification, UserInfoHeader> None { get; }
        = new Dictionary<UserIdentification, UserInfoHeader>();

    /// <param name="overrideValues">
    /// Значения из формы, которые перебьют сохранённые (перерисовка формы после ошибки) —
    /// пользователей надо грузить по ним, иначе только что введённая ссылка покажется удалённой.
    /// </param>
    public static async Task<IReadOnlyDictionary<UserIdentification, UserInfoHeader>> LoadFieldUserLinks(
        this IUserRepository userRepository,
        IEnumerable<FieldWithValue> fields,
        Dictionary<int, string?>? overrideValues = null)
    {
        IReadOnlyCollection<UserIdentification> userIds =
            [.. fields.SelectMany(f => UserIdsWithOverride(f, overrideValues)).Distinct()];

        return await userRepository.LoadUserLinks(userIds);
    }

    /// <summary>
    /// Резолв уже собранного набора идентификаторов. Нужен там, где id берутся не прямо из полей
    /// экрана — например ведущие пунктов программы в расписании (#4512).
    /// </summary>
    public static async Task<IReadOnlyDictionary<UserIdentification, UserInfoHeader>> LoadUserLinks(
        this IUserRepository userRepository,
        IReadOnlyCollection<UserIdentification> userIds)
    {
        if (userIds.Count == 0)
        {
            return None;
        }

        // Именно GetUserInfoHeaders, а не GetRequired...: удалённый пользователь не должен
        // ронять весь экран, вместо ссылки покажем «пользователь удалён».
        return (await userRepository.GetUserInfoHeaders(userIds)).ToDictionary(user => user.UserId);
    }

    /// <summary>
    /// Ссылки на пользователей из значения поля — по словарю, загруженному на весь экран.
    /// </summary>
    /// <remarks>
    /// Пользователь, которого нет в словаре, удалён (или id в значении — мусор):
    /// ссылки не будет, но запись поля из-за этого не пропадает.
    /// Здесь нельзя collection expression ([.. ...]): для IReadOnlyList<T> компилятор создаёт
    /// внутренний тип &lt;&gt;z__ReadOnlyList&lt;T&gt; в этой сборке, а список уезжает параметром
    /// InitialUsers в WASM-остров JoinUserLinkEditor. Параметры острова сериализуются вместе с
    /// именем рантайм-типа, и клиент такой тип найти не может — остров падает на старте
    /// («could not be found»), страница заявки ломается.
    /// </remarks>
    public static IReadOnlyList<UserLinkViewModel> GetUserLinks(
        this IReadOnlyDictionary<UserIdentification, UserInfoHeader> users,
        FieldWithValue field)
        => field.UserIds.Select(userId =>
            users.TryGetValue(userId, out var user)
                ? new UserLinkViewModel(user)
                : UserLinkViewModel.Deleted).ToList();

    /// <remarks>
    /// Значение из формы накладывается на новый <see cref="FieldWithValue"/>, а не на переданный:
    /// <c>GetAllFieldsForEdit</c> отдаёт объекты прямо из слоёв агрегата, и правка на месте
    /// протекла бы во всех, кто читает тот же агрегат в этом запросе.
    /// </remarks>
    private static IReadOnlyList<UserIdentification> UserIdsWithOverride(
        FieldWithValue field,
        Dictionary<int, string?>? overrideValues)
    {
        if (overrideValues?.TryGetValue(field.Field.Id.ProjectFieldId, out var overrideValue) == true)
        {
            return new FieldWithValue(field.Field, overrideValue).UserIds;
        }
        return field.UserIds;
    }

    public static Task<IReadOnlyDictionary<UserIdentification, UserInfoHeader>> LoadFieldUserLinks(
        this IUserRepository userRepository,
        Character character,
        ProjectInfo projectInfo)
        => userRepository.LoadFieldUserLinks(character.GetFields(projectInfo));

    public static Task<IReadOnlyDictionary<UserIdentification, UserInfoHeader>> LoadFieldUserLinks(
        this IUserRepository userRepository,
        CharacterInfo character,
        Dictionary<int, string?>? overrideValues = null)
        => userRepository.LoadFieldUserLinks(character.GetAllFields(), overrideValues);

    /// <remarks>
    /// Один и тот же пользователь встречается и в разных полях, и у разных персонажей пачки. Схлопывает
    /// это общая перегрузка по полям: <c>Distinct</c> там стоит на уровне id пользователей, уже после
    /// разбора значений, так что в репозиторий уходит один запрос с уникальным набором id.
    /// </remarks>
    public static Task<IReadOnlyDictionary<UserIdentification, UserInfoHeader>> LoadFieldUserLinks(
        this IUserRepository userRepository,
        IEnumerable<CharacterInfo> characters)
        => userRepository.LoadFieldUserLinks(characters.SelectMany(c => c.GetAllFields()));

    public static Task<IReadOnlyDictionary<UserIdentification, UserInfoHeader>> LoadFieldUserLinks(
        this IUserRepository userRepository,
        Claim claim,
        ProjectInfo projectInfo)
        => userRepository.LoadFieldUserLinks(claim.GetFields(projectInfo));
}
