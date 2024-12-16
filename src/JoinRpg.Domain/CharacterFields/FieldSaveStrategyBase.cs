using JoinRpg.DataModel;
using JoinRpg.Domain.Access;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Domain.CharacterFields;

internal abstract class FieldSaveStrategyBase(Claim? claim,
    Character? character,
    int currentUserId,
    IFieldDefaultValueGenerator generator,
    ProjectInfo projectInfo)
{
    protected Claim? Claim { get; } = claim;
    protected Character? Character { get; } = character;
    protected Project Project { get; } = character?.Project ?? claim?.Project ?? throw new ArgumentNullException("",
                "Either character or claim should be not null");

    protected ProjectInfo ProjectInfo { get; } = projectInfo;

    private List<FieldWithPreviousAndNewValue> UpdatedFields { get; } =
        new List<FieldWithPreviousAndNewValue>();

    public IReadOnlyCollection<FieldWithPreviousAndNewValue> GetUpdatedFields() =>
        UpdatedFields.Where(uf => uf.PreviousDisplayString != uf.DisplayString).ToList();

    public virtual void Save(Dictionary<int, FieldWithValue> fields) => SerializeFields(fields);

    protected abstract void SerializeFields(Dictionary<int, FieldWithValue> fields);

    public abstract IReadOnlyCollection<FieldWithValue> GetFields();

    public void EnsureEditAccess(FieldWithValue field)
    {
        var accessArguments = Character != null
            ? AccessArgumentsFactory.Create(Character, currentUserId)
            : AccessArgumentsFactory.Create(Claim!, currentUserId); // Either character or claim should be not null

        var editAccess = field.Field.HasEditAccess(accessArguments);
        if (!editAccess)
        {
            throw new NoAccessToProjectException(Project, currentUserId);
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

        return true;
    }

    public string? GenerateDefaultValue(FieldWithValue field)
    {
        return field.Field.BoundTo switch
        {
            FieldBoundTo.Character => generator.CreateDefaultValue(Character, field),
            FieldBoundTo.Claim => generator.CreateDefaultValue(Claim, field),
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    protected abstract void SetCharacterNameFromPlayer();
}
