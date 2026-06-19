using JoinRpg.Common.PrimitiveTypes.Users;

namespace JoinRpg.DomainTypes.ProjectMetadata;

public record CreateUpdateMarksInfo(
    DateTime CreatedAt,
    UserInfoHeader? CreatedBy,
    DateTime UpdatedAt,
    UserInfoHeader? UpdatedBy
);
