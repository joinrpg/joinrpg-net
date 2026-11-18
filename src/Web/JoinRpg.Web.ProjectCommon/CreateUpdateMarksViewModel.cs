namespace JoinRpg.Web.ProjectCommon;

public record CreateUpdateMarksViewModel(
    DateTime CreatedAt,
    UserLinkViewModel? CreatedBy,
    DateTime UpdatedAt,
    UserLinkViewModel? UpdatedBy
);
