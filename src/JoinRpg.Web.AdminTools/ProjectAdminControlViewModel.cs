using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Web.AdminTools;
public record class ProjectAdminControlViewModel(ProjectIdentification ProjectId, ProjectName ProjectName, KogdaIgraIdentification[] KogdaIgraLinkedIds);
