using System.Text.Json;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Accommodation;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.Web.Models.Accommodation;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.Accommodation.Test;

/// <summary>
/// Форма JSON у группы проживающих — часть контракта с <c>wwwroot/Scripts/rooms.js</c>: скрипт читает
/// свойства по именам, поэтому тест фиксирует их целиком, а не по одному.
/// </summary>
public class AccRequestJsonTest
{
    private static readonly ProjectIdentification ProjectId = new(123);

    [Fact]
    public void SerializedShapeIsWhatRoomsJsExpects()
    {
        var json = AccRequestJson.Serialize([MakeRequest(roomId: 7)]);

        // Имена — PascalCase, [JsonIgnore]-свойства (ProjectId, AccommodationTypeId, Participants)
        // в выводе отсутствуют, порядок — объявления. Кириллица экранирована стандартным
        // JavaScriptEncoder: значение уезжает и в HTML-атрибут, и внутрь <script> через Html.Raw.
        json.ShouldBe(
            """
            [{"Id":42,"RoomId":7,"Persons":2,"PersonsList":"\u0418\u0432\u0430\u043D, \u041F\u0435\u0442\u0440","Instance":null,"FeeTotal":2000,"FeeToPay":500,"PaymentStatusCssClass":"warning","PaymentStatusTitle":"\u041D\u0435 \u043E\u043F\u043B\u0430\u0447\u0435\u043D\u043E 500 \u0438\u0437 2000"}]
            """);
    }

    [Fact]
    public void UnassignedGroupHasZeroRoomId()
    {
        // Ноль как признак нерасселённой группы — rooms.js проверяет req.RoomId > 0 и сбрасывает
        // поле в 0 при выселении. null бы этот контракт сломал.
        var json = AccRequestJson.Serialize([MakeRequest(roomId: null)]);

        JsonDocument.Parse(json).RootElement[0].GetProperty("RoomId").GetInt32().ShouldBe(0);
    }

    [Fact]
    public void CyrillicSurvivesRoundTrip()
    {
        // Экранирование \uXXXX — это всё ещё тот же JSON: rooms.js прогоняет значение через eval,
        // и в DOM уезжают настоящие буквы, а не последовательности.
        var json = AccRequestJson.Serialize([MakeRequest(roomId: 7)]);

        JsonDocument.Parse(json).RootElement[0].GetProperty("PersonsList").GetString()
            .ShouldBe("Иван, Петр");
    }

    private static AccRequestViewModel MakeRequest(int? roomId)
    {
        var group = new AccommodationGroupInfo(
            new AccommodationRequestIdentification(ProjectId, 42),
            new AccommodationTypeIdentification(ProjectId, 11),
            roomId is null ? null : new AccommodationRoomIdentification(ProjectId, roomId.Value),
            [new ClaimIdentification(ProjectId, 1), new ClaimIdentification(ProjectId, 2)]);

        return new AccRequestViewModel(
            group,
            [
                new RequestParticipantViewModel(
                    new ClaimIdentification(ProjectId, 1), new UserIdentification(1), "Иван", 1000, 500),
                new RequestParticipantViewModel(
                    new ClaimIdentification(ProjectId, 2), new UserIdentification(2), "Петр", 1000, 0),
            ]);
    }
}
