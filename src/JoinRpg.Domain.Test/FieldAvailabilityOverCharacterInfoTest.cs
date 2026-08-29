using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain.Problems.CommonProblemFilters;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Domain.Test;

/// <summary>
/// Доступность поля и построенные поверх неё проблемы должны считаться одинаково для доменного
/// агрегата <see cref="CharacterInfo"/> (ADR013) и для EF-сущности. Пока обе реализации живы,
/// это единственное место, где расхождение станет видно.
/// </summary>
public class FieldAvailabilityOverCharacterInfoTest
{
    private readonly MockedProject mock = new();
    private readonly CharacterGroup otherGroup;

    public FieldAvailabilityOverCharacterInfoTest()
    {
        otherGroup = mock.CreateCharacterGroup();
        mock.ReInitProjectInfo();
    }

    public static TheoryData<bool, bool, FieldBoundTo, CharacterType, bool> Cases()
    {
        var data = new TheoryData<bool, bool, FieldBoundTo, CharacterType, bool>();
        foreach (var fieldIsActive in new[] { true, false })
        {
            foreach (var validForNpc in new[] { true, false })
            {
                foreach (var boundTo in new[] { FieldBoundTo.Character, FieldBoundTo.Claim })
                {
                    foreach (var characterType in Enum.GetValues<CharacterType>())
                    {
                        foreach (var limitToOtherGroup in new[] { true, false })
                        {
                            data.Add(fieldIsActive, validForNpc, boundTo, characterType, limitToOtherGroup);
                        }
                    }
                }
            }
        }
        return data;
    }

    [Theory, MemberData(nameof(Cases))]
    public void AvailabilityShouldMatchEntityCalculation(
        bool fieldIsActive,
        bool validForNpc,
        FieldBoundTo boundTo,
        CharacterType characterType,
        bool limitToOtherGroup)
    {
        var field = MakeField(fieldIsActive, validForNpc, boundTo, limitToOtherGroup);
        var (entityTarget, aggregate) = MakePair(characterType);

        field.IsAvailableForTarget(aggregate)
            .ShouldBe(field.IsAvailableForTarget(entityTarget));
    }

    [Theory, MemberData(nameof(Cases))]
    public void FieldNotSetProblemsShouldMatchEntityCalculation(
        bool fieldIsActive,
        bool validForNpc,
        FieldBoundTo boundTo,
        CharacterType characterType,
        bool limitToOtherGroup)
    {
        var field = MakeField(fieldIsActive, validForNpc, boundTo, limitToOtherGroup);
        var (entityTarget, aggregate) = MakePair(characterType);
        var fieldWithValue = new FieldWithValue(field, value: null);
        var filter = new FieldNotSetFilter();

        filter.CheckField(aggregate, fieldWithValue).ToArray()
            .ShouldBe(filter.CheckField(entityTarget, fieldWithValue).ToArray());
    }

    [Fact]
    public void GroupRestrictionIsActuallyChecked()
    {
        // Страж от вырожденного сравнения выше: если бы ограничение по группам не работало,
        // обе реализации согласованно возвращали бы true и тесты ничего бы не проверили.
        // Поля добавляются до сборки агрегата: AddField пересоздаёт ProjectInfo, а агрегат
        // привязан к конкретному экземпляру.
        var restricted = MakeField(true, true, FieldBoundTo.Character, limitToOtherGroup: true);
        var unrestricted = MakeField(true, true, FieldBoundTo.Character, limitToOtherGroup: false);
        var (_, aggregate) = MakePair(CharacterType.Player);

        restricted.IsAvailableForTarget(aggregate).ShouldBeFalse();
        unrestricted.IsAvailableForTarget(aggregate).ShouldBeTrue();
    }

    private ProjectFieldInfo MakeField(bool isActive, bool validForNpc, FieldBoundTo boundTo, bool limitToOtherGroup)
        => mock.AddField(field =>
        {
            field.FieldType = ProjectFieldType.String;
            field.FieldBoundTo = boundTo;
            field.IsActive = isActive;
            field.ValidForNpc = validForNpc;
            field.MandatoryStatus = MandatoryStatus.Required;
            field.AvailableForCharacterGroupIds = limitToOtherGroup ? [otherGroup.CharacterGroupId] : [];
        });

    /// <summary>
    /// Один и тот же персонаж в двух представлениях: обёртка над сущностью и доменный агрегат.
    /// </summary>
    private (CharacterItem Entity, CharacterInfo Aggregate) MakePair(CharacterType characterType)
    {
        var projectInfo = mock.ProjectInfo;

        var character = mock.CreateCharacter($"Персонаж{characterType}");
        character.CharacterType = characterType;
        // CharacterTypeInfo не разрешает лимит слотов не-слоту, а сущность про это не знает.
        character.CharacterSlotLimit = characterType == CharacterType.Slot ? 1 : null;

        var entity = new CharacterBulkLoader().LoadCharacter(character, projectInfo);

        var aggregate = new CharacterInfo(
            new CharacterIdentification(projectInfo.ProjectId, character.CharacterId),
            projectInfo,
            character.CharacterName,
            new CharacterTypeInfo(
                characterType,
                IsHot: false,
                character.CharacterSlotLimit,
                SlotName: null,
                CharacterVisibility.Public),
            hidePlayerForCharacter: false,
            isActive: true,
            inGame: false,
            autoCreated: false,
            new MarkdownString(""),
            originalCharacterSlotId: null,
            [.. character.GetDirectGroupIds()],
            FieldLayerContainer.DeserializeFieldLayer(projectInfo, null),
            [],
            approvedClaimId: null,
            DateTime.UtcNow,
            new UserIdentification(mock.Master.UserId),
            DateTime.UtcNow,
            new UserIdentification(mock.Master.UserId));

        return (entity, aggregate);
    }
}
