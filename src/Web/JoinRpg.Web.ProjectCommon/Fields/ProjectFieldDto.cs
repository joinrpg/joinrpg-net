namespace JoinRpg.Web.ProjectCommon.Fields;

public record ProjectFieldDto(
    ProjectFieldIdentification FieldId,
    string Name,
    ProjectFieldType FieldType,
    FieldBoundTo BoundTo);
