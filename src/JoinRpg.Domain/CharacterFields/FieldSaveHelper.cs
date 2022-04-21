using JetBrains.Annotations;
using JoinRpg.DataModel;

namespace JoinRpg.Domain.CharacterFields;

/// <summary>
/// Saves fields either to character or to claim
/// </summary>
//TODO That should be service with interface and costructed via DI container
public static class FieldSaveHelper
{
    private abstract class FieldSaveStrategyBase
    {
        protected Claim? Claim { get; }
        protected Character? Character { get; }
        private int CurrentUserId { get; }
        private IFieldDefaultValueGenerator Generator { get; }
        protected Project Project { get; }

        private List<FieldWithPreviousAndNewValue> UpdatedFields { get; } =
            new List<FieldWithPreviousAndNewValue>();

        protected FieldSaveStrategyBase(Claim? claim,
            Character? character,
            int currentUserId,
            IFieldDefaultValueGenerator generator)
        {
            Claim = claim;
            Character = character;
            CurrentUserId = currentUserId;
            Generator = generator;
            Project = character?.Project ?? claim?.Project ?? throw new ArgumentNullException("",
                    "Either character or claim should be not null");
        }

        public IReadOnlyCollection<FieldWithPreviousAndNewValue> GetUpdatedFields() =>
            UpdatedFields.Where(uf => uf.PreviousDisplayString != uf.DisplayString).ToList();

        public abstract void Save(Dictionary<int, FieldWithValue> fields);

        public Dictionary<int, FieldWithValue> LoadFields()
        {
            var fields =
                Project.GetFieldsNotFilledWithoutOrder()
                    .ToList()
                    .FillIfEnabled(Claim, Character)
                    .ToDictionary(f => f.Field.ProjectFieldId);
            return fields;
        }

        public void EnsureEditAccess(FieldWithValue field)
        {
            var accessArguments = Character != null
                ? new AccessArguments(Character, CurrentUserId)
                : new AccessArguments(Claim!, CurrentUserId); // Either character or claim should be not null

            var editAccess = field.HasEditAccess(accessArguments);
            if (!editAccess)
            {
                throw new NoAccessToProjectException(Project, CurrentUserId);
            }
        }

        /// <summary>
        /// Returns true is the value has changed
        /// </summary>
        public bool AssignFieldValue(FieldWithValue field, string? newValue)
        {
            if (field.Value == newValue)
            {
                return false;
            }

            var existingField = UpdatedFields.FirstOrDefault(uf => uf.Field == field.Field);
            if (existingField != null)
            {
                existingField.Value = newValue;
            }
            else
            {
                UpdatedFields.Add(
                    new FieldWithPreviousAndNewValue(field.Field, newValue, field.Value));
            }

            field.Value = newValue;
            field.MarkUsed();

            return true;
        }

        public string? GenerateDefaultValue(FieldWithValue field)
        {
            return field.Field.FieldBoundTo switch
            {
                FieldBoundTo.Character => Generator.CreateDefaultValue(Character, field),
                FieldBoundTo.Claim => Generator.CreateDefaultValue(Claim, field),
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        protected abstract void SetCharacterNameFromPlayer();
    }

    private abstract class CharacterExistsStrategyBase : FieldSaveStrategyBase
    {
        protected new Character Character => base.Character!; //Character should always exists

        protected CharacterExistsStrategyBase(Claim? claim, Character character, int currentUserId, IFieldDefaultValueGenerator generator)
            : base(claim, character, currentUserId, generator)
        {
        }

        protected void UpdateSpecialGroups(Dictionary<int, FieldWithValue> fields)
        {
            var ids = fields.Values.GenerateSpecialGroupsList();
            var groupsToKeep = Character.Groups.Where(g => !g.IsSpecial)
                .Select(g => g.CharacterGroupId);
            Character.ParentCharacterGroupIds = groupsToKeep.Union(ids).ToArray();
        }

        protected void SetCharacterDescription(Dictionary<int, FieldWithValue> fields)
        {
            if (Project.Details.CharacterDescription != null)
            {
                Character.Description = new MarkdownString(
                    fields[Project.Details.CharacterDescription.ProjectFieldId].Value);
            }

            if (!Project.Details.CharacterNameLegacyMode)
            {
                if (Project.Details.CharacterNameField == null)
                {
                    SetCharacterNameFromPlayer();
                }
                else
                {
                    var name = fields[Project.Details.CharacterNameField.ProjectFieldId].Value;

                    Character.CharacterName = string.IsNullOrWhiteSpace(name) ?
                        Character.CharacterName = "CHAR" + Character.CharacterId
                        : name;
                }
            }
        }
    }

    private class SaveToCharacterOnlyStrategy : CharacterExistsStrategyBase
    {
        public SaveToCharacterOnlyStrategy(
            Character character,
            int currentUserId,
            IFieldDefaultValueGenerator generator)
            : base(claim: null,
            character,
            currentUserId,
            generator)
        {
        }

        public override void Save(Dictionary<int, FieldWithValue> fields)
        {
            Character.JsonData = fields.Values
                .Where(v => v.Field.FieldBoundTo == FieldBoundTo.Character).SerializeFields();
            SetCharacterDescription(fields);

            UpdateSpecialGroups(fields);
        }

        protected override void SetCharacterNameFromPlayer()
        {
            //TODO: we don't have player yet, but have to set player name from it.
            //M.b. Disallow create characters in this scenarios?
            Character.CharacterName = Character.CharacterName ?? "PLAYER_NAME";
        }
    }

    private class SaveToClaimOnlyStrategy : FieldSaveStrategyBase
    {
        protected new Claim Claim => base.Claim!; //Claim should always exists

        public SaveToClaimOnlyStrategy(Claim claim,
            int currentUserId,
            IFieldDefaultValueGenerator generator) : base(claim,
            character: null,
            currentUserId,
            generator)
        {
        }

        public override void Save(Dictionary<int, FieldWithValue> fields)
        {
            //TODO do not save fields that have values same as character's
            Claim.JsonData = fields.Values.SerializeFields();
        }

        protected override void SetCharacterNameFromPlayer()
        {
            //Do nothing player could not change character yet
        }
    }

    private class SaveToCharacterAndClaimStrategy : CharacterExistsStrategyBase
    {
        protected new Claim Claim => base.Claim!; //Claim should always exists

        public SaveToCharacterAndClaimStrategy(Claim claim,
            Character character,
            int currentUserId,
            IFieldDefaultValueGenerator generator) : base(claim,
            character,
            currentUserId,
            generator)
        {
        }

        public override void Save(Dictionary<int, FieldWithValue> fields)
        {
            Character.JsonData = fields.Values
                .Where(v => v.Field.FieldBoundTo == FieldBoundTo.Character).SerializeFields();

            Claim.JsonData = fields.Values
                .Where(v => v.Field.FieldBoundTo == FieldBoundTo.Claim).SerializeFields();

            SetCharacterDescription(fields);

            UpdateSpecialGroups(fields);
        }

        protected override void SetCharacterNameFromPlayer() => Character.CharacterName = Claim.Player.GetDisplayName();
    }

    /// <summary>
    /// Saves character fields
    /// </summary>
    /// <returns>Fields that have changed.</returns>
    [MustUseReturnValue]
    public static IReadOnlyCollection<FieldWithPreviousAndNewValue> SaveCharacterFields(
        int currentUserId,
        [NotNull]
        Claim claim,
        [NotNull]
        IReadOnlyDictionary<int, string?> newFieldValue,
        IFieldDefaultValueGenerator generator)
    {
        if (claim == null)
        {
            throw new ArgumentNullException(nameof(claim));
        }

        return SaveCharacterFieldsImpl(currentUserId,
            claim.Character,
            claim,
            newFieldValue,
            generator);
    }

    /// <summary>
    /// Saves fields of a character
    /// </summary>
    /// <returns>The list of updated fields</returns>
    [MustUseReturnValue]
    public static IReadOnlyCollection<FieldWithPreviousAndNewValue> SaveCharacterFields(
        int currentUserId,
        [NotNull]
        Character character,
        IReadOnlyDictionary<int, string?> newFieldValue,
        IFieldDefaultValueGenerator generator)
    {
        if (character == null)
        {
            throw new ArgumentNullException(nameof(character));
        }

        return SaveCharacterFieldsImpl(currentUserId,
            character,
            character.ApprovedClaim,
            newFieldValue,
            generator);
    }

    [MustUseReturnValue]
    private static IReadOnlyCollection<FieldWithPreviousAndNewValue> SaveCharacterFieldsImpl(
        int currentUserId,
        Character? character,
        Claim? claim,
        [NotNull]
        IReadOnlyDictionary<int, string?> newFieldValue,
        IFieldDefaultValueGenerator generator)
    {
        if (newFieldValue == null)
        {
            throw new ArgumentNullException(nameof(newFieldValue));
        }

        var strategy = CreateStrategy(currentUserId, character, claim, generator);

        var fields = strategy.LoadFields();

        AssignValues(newFieldValue, fields, strategy);

        GenerateDefaultValues(character, fields, strategy);

        strategy.Save(fields);
        return strategy.GetUpdatedFields();
    }

    private static FieldSaveStrategyBase CreateStrategy(int currentUserId, Character? character,
        Claim? claim, IFieldDefaultValueGenerator generator)
    {
        return (claim, character) switch
        {
            (null, Character ch) => new SaveToCharacterOnlyStrategy(ch, currentUserId, generator),
            ({ IsApproved: true }, Character ch) => new SaveToCharacterAndClaimStrategy(claim!, ch, currentUserId, generator),
            ({ IsApproved: false }, _) => new SaveToClaimOnlyStrategy(claim!, currentUserId, generator),
            _ => throw new InvalidOperationException("Either claim or character should be correct"),
        };
    }

    private static void AssignValues(IReadOnlyDictionary<int, string?> newFieldValue, Dictionary<int, FieldWithValue> fields,
        FieldSaveStrategyBase strategy)
    {
        foreach (var keyValuePair in newFieldValue)
        {
            var field = fields[keyValuePair.Key];

            strategy.EnsureEditAccess(field);

            var normalizedValue = NormalizeValueBeforeAssign(field, keyValuePair.Value);

            if (normalizedValue is null && field.Field.MandatoryStatus == MandatoryStatus.Required)
            {
                throw new FieldRequiredException(field.Field.FieldName);
            }

            _ = strategy.AssignFieldValue(field, normalizedValue);
        }
    }

    private static void GenerateDefaultValues(Character? character, Dictionary<int, FieldWithValue> fields,
        FieldSaveStrategyBase strategy)
    {
        foreach (var field in fields.Values.Where(
            f => !f.HasEditableValue && f.Field.CanHaveValue() &&
                 f.Field.IsAvailableForTarget(character)))
        {
            var newValue = strategy.GenerateDefaultValue(field);

            var normalizedValue = NormalizeValueBeforeAssign(field, newValue);

            _ = strategy.AssignFieldValue(field, normalizedValue);
        }
    }

    private static string? NormalizeValueBeforeAssign(FieldWithValue field, string? toAssign)
    {
        switch (field.Field.FieldType)
        {
            case ProjectFieldType.Checkbox:
                return toAssign?.StartsWith(FieldWithValue.CheckboxValueOn) == true
                    ? FieldWithValue.CheckboxValueOn
                    : "";
            default:
                return string.IsNullOrEmpty(toAssign) ? null : toAssign;
        }
    }
}
