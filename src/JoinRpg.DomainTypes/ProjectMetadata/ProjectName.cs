namespace JoinRpg.DomainTypes.ProjectMetadata;

/// <summary>
/// Название проекта. Длина проверяется после обрезки пробелов — при создании из строки (<c>Parse</c>, неявное приведение);
/// <c>new ProjectName(...)</c> не проверяет, им читаются старые названия из БД, которые бывают короче и длиннее.
/// </summary>
[TypedStringValue(MinLength = 5, MaxLength = 60)]
public partial record ProjectName(string Value);
