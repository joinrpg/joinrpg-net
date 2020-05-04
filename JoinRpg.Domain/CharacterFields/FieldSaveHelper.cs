using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Helpers;

namespace JoinRpg.Domain.CharacterFields
{
    /// <summary>
    /// Saves fields either to character or to claim
    /// </summary>
    //TODO That should be service with interface and costructed via DI container
    public static class FieldSaveHelper
    {
        private abstract class FieldSaveStrategyBase
        {
            protected Claim Claim { get; }
            protected Character Character { get; }
            private int CurrentUserId { get; }
            private IFieldDefaultValueGenerator Generator { get; }
            private Project Project { get; }

            private List<FieldWithPreviousAndNewValue> UpdatedFields { get; } =
                new List<FieldWithPreviousAndNewValue>();

            protected FieldSaveStrategyBase(Claim claim,
                Character character,
                int currentUserId,
                IFieldDefaultValueGenerator generator)
            {
                Claim = claim;
                Character = character;
                CurrentUserId = currentUserId;
                Generator = generator;
                Project = character?.Project ?? claim?.Project;

                if (Project == null)
                {
                    throw new ArgumentNullException("",
                        "Either character or claim should be not null");
                }
            }

            public IReadOnlyCollection<FieldWithPreviousAndNewValue> GetUpdatedFields() =>
                UpdatedFields.Where(uf => uf.PreviousDisplayString != uf.DisplayString).ToList();

            public abstract void Save(Dictionary<int, FieldWithValue> fields);

            protected void UpdateSpecialGroups(Dictionary<int, FieldWithValue> fields)
            {
                var ids = fields.Values.GenerateSpecialGroupsList();
                var groupsToKeep = Character.Groups.Where(g => !g.IsSpecial)
                    .Select(g => g.CharacterGroupId);
                Character.ParentCharacterGroupIds = groupsToKeep.Union(ids).ToArray();
            }

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
                    : new AccessArguments(Claim, CurrentUserId);

                var editAccess = field.HasEditAccess(accessArguments);
                if (!editAccess)
                {
                    throw new NoAccessToProjectException(Project, CurrentUserId);
                }
            }

            /// <summary>
            /// Returns true is the value has changed
            /// </summary>
            public bool AssignFieldValue(FieldWithValue field, string newValue)
            {
                if (field.Value == newValue) return false;

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

            public string GenerateDefaultValue(FieldWithValue field)
            {
                string newValue;
                switch (field.Field.FieldBoundTo)
                {
                    case FieldBoundTo.Character:
                        newValue = Generator.CreateDefaultValue(Character, field);
                        break;
                    case FieldBoundTo.Claim:
                        newValue = Generator.CreateDefaultValue(Claim, field);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                return newValue;
            }

            protected void SetCharacterDescription(Dictionary<int, FieldWithValue> fields)
            {
                if (Project.Details.CharacterDescription != null)
                {
                    Character.Description = new MarkdownString(
                        fields
                            .GetValueOrDefault(Project.Details.CharacterDescription.ProjectFieldId)
                            .Value);
                }

                if (!Project.Details.CharacterNameLegacyMode)
                {
                    if (Project.Details.CharacterNameField == null)
                    {
                        SetCharacterNameFromPlayer();
                    }
                    else
                    {
                        Character.CharacterName = fields
                            .GetValueOrDefault(Project.Details.CharacterNameField.ProjectFieldId)
                            .Value;

                        if (string.IsNullOrWhiteSpace(Character.CharacterName))
                        {
                            Character.CharacterName = "CHAR" + Character.CharacterId;
                        }
                    }
                }
            }

            protected abstract void SetCharacterNameFromPlayer();
        }

        private class SaveToCharacterOnlyStrategy : FieldSaveStrategyBase
        {
            public SaveToCharacterOnlyStrategy(Claim claim,
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
            public SaveToClaimOnlyStrategy(Claim claim,
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
                //TODO do not save fields that have values same as character's
                Claim.JsonData = fields.Values.SerializeFields();
            }

            protected override void SetCharacterNameFromPlayer()
            {
                //Do nothing player could not change character yet
            }
        }

        private class SaveToCharacterAndClaimStrategy : FieldSaveStrategyBase
        {
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

            protected override void SetCharacterNameFromPlayer()
            {
                Character.CharacterName = Claim.Player.GetDisplayName();
            }
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
            IReadOnlyDictionary<int, string> newFieldValue,
            IFieldDefaultValueGenerator generator)
        {
            if (claim == null) throw new ArgumentNullException(nameof(claim));
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
            [NotNull]
            IReadOnlyDictionary<int, string> newFieldValue,
            IFieldDefaultValueGenerator generator)
        {
            if (character == null) throw new ArgumentNullException(nameof(character));
            return SaveCharacterFieldsImpl(currentUserId,
                character,
                character.ApprovedClaim,
                newFieldValue,
                generator);
        }

        [MustUseReturnValue]
        private static IReadOnlyCollection<FieldWithPreviousAndNewValue> SaveCharacterFieldsImpl(
            int currentUserId,
            [CanBeNull]
            Character character,
            [CanBeNull]
            Claim claim,
            [NotNull]
            IReadOnlyDictionary<int, string> newFieldValue,
            IFieldDefaultValueGenerator generator)
        {
            if (newFieldValue == null) throw new ArgumentNullException(nameof(newFieldValue));

            var strategy = CreateStrategy(currentUserId, character, claim, generator);

            var fields = strategy.LoadFields();

            AssignValues(newFieldValue, fields, strategy);

            GenerateDefaultValues(character, fields, strategy);

            strategy.Save(fields);
            return strategy.GetUpdatedFields();
        }

        private static FieldSaveStrategyBase CreateStrategy(int currentUserId, Character character,
            Claim claim, IFieldDefaultValueGenerator generator)
        {
            FieldSaveStrategyBase strategy;
            if (claim == null)
            {
                strategy =
                    new SaveToCharacterOnlyStrategy(null, character, currentUserId, generator);
            }
            else if (!claim.IsApproved)
            {
                strategy = new SaveToClaimOnlyStrategy(claim, null, currentUserId, generator);
            }
            else
            {
                strategy =
                    new SaveToCharacterAndClaimStrategy(claim, character, currentUserId, generator);
            }

            return strategy;
        }

        private static void AssignValues(IReadOnlyDictionary<int, string> newFieldValue, Dictionary<int, FieldWithValue> fields,
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

                strategy.AssignFieldValue(field, normalizedValue);
            }
        }

        private static void GenerateDefaultValues(Character character, Dictionary<int, FieldWithValue> fields,
            FieldSaveStrategyBase strategy)
        {
            foreach (var field in fields.Values.Where(
                f => !f.HasEditableValue && f.Field.CanHaveValue() &&
                     f.Field.IsAvailableForTarget(character)))
            {
                var newValue = strategy.GenerateDefaultValue(field);

                var normalizedValue = NormalizeValueBeforeAssign(field, newValue);

                strategy.AssignFieldValue(field, normalizedValue);
            }
        }

        private static string NormalizeValueBeforeAssign(FieldWithValue field, string toAssign)
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
}
