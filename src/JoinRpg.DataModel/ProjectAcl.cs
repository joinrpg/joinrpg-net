using System.ComponentModel.DataAnnotations;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Helpers;

namespace JoinRpg.DataModel;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global (required by LINQ)
public class ProjectAcl : IProjectEntity
{
    public int ProjectAclId { get; set; }
    public int ProjectId { get; set; }

    public virtual Project Project { get; set; }

    public int UserId { get; set; }

    public virtual User User { get; set; }

    /// <summary>
    /// Project's Unique Token
    /// </summary>
    /// <remarks>
    /// Token automatically assigns at creating ProjectAcl instance
    /// </remarks>
    public Guid Token { get; set; } = Guid.NewGuid();

    public bool CanChangeFields { get; set; }

    public bool CanChangeProjectProperties { get; set; }

    public bool IsOwner { get; set; }

    public bool CanGrantRights { get; set; }

    public bool CanManageClaims { get; set; }

    public bool CanEditRoles { get; set; }
    public bool CanManageMoney { get; set; }

    public bool CanSendMassMails { get; set; }

    public bool CanManagePlots { get; set; }

    public bool CanManageAccommodation { get; set; }

    public bool CanSetPlayersAccommodations { get; set; }

    /// <summary>
    /// Доступ даёт только <see cref="ProjectAclStatus.Active"/> (ADR019). Снятого мастера не удаляем, а помечаем.
    /// </summary>
    public ProjectAclStatus Status { get; set; } = ProjectAclStatus.Active;

    public bool IsActive => Status == ProjectAclStatus.Active;

    /// <summary>
    /// Показывать ли мастера немастерам (главная страница проекта, страница мастеров).
    /// </summary>
    public bool IsPublic { get; set; } = true;

    /// <summary>
    /// Роль мастера в проекте — свободный текст: «Главный мастер», «Мастер по боёвке».
    /// </summary>
    [Required, MaxLength(100)]
    public required string Role { get; set; }

    /// <summary>
    /// Чем мастер занимается в проекте и по каким вопросам ему писать.
    /// </summary>
    public MarkdownDbValue Description { get; set; } = new MarkdownDbValue();

    public static ProjectAcl CreateRootAcl(int userId, string role, bool isOwner = false)
    {
        return new ProjectAcl
        {
            Role = role,
            CanChangeFields = true,
            CanChangeProjectProperties = true,
            UserId = userId,
            IsOwner = isOwner,
            CanGrantRights = true,
            CanManageClaims = true,
            CanEditRoles = true,
            CanManageMoney = true,
            CanSendMassMails = true,
            CanManagePlots = true,
            CanManageAccommodation = true,
            CanSetPlayersAccommodations = true,
        };
    }

    int IOrderableEntity.Id => ProjectAclId;
}
