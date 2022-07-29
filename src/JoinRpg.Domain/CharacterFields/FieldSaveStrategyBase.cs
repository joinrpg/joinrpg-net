using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Domain.CharacterFields;

internal abstract class FieldSaveStrategyBase
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
            ? AccessArgumentsFactory.Create(Character, CurrentUserId)
            : AccessArgumentsFactory.Create(Claim!, CurrentUserId); // Either character or claim should be not null

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
