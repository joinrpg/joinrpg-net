namespace JoinRpg.PrimitiveTypes.ProjectMetadata;

public record ProjectName : SingleValueType<string>
{
    public ProjectName(string value) : base(value.Trim())
    {
        if (Value.Length > 100)
        {
            throw new ArgumentException("Project name is too long", nameof(value));
        }
        if (Value.Length < 5)
        {
            throw new ArgumentException("Project name is too long", nameof(value));
        }
    }
}
