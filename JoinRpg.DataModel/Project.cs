using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.Helpers;

namespace JoinRpg.DataModel
{
  // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global (used by LINQ)
  public class Project : IProjectEntity
  {
    public int ProjectId { get; set; }

    int IOrderableEntity.Id => ProjectId;

    public string ProjectName { get; set; }

    public DateTime CreatedDate { get; set; }

    public bool Active { get; set; }

    public bool IsAcceptingClaims { get; set; }

    public virtual ICollection<ProjectAcl> ProjectAcls { get; set; }

    public virtual ICollection<ForumThread> ForumThreads { get; set; }

    public virtual ICollection<ProjectField> ProjectFields { get; set; }

    public virtual ICollection<CharacterGroup>  CharacterGroups { get; set; }

    public virtual ICollection<Character>  Characters { get; set; }

    public virtual ICollection<Claim> Claims { get; set; }

    public virtual ICollection<FinanceOperation> FinanceOperations { get; set; }

    [NotNull]
    public virtual ProjectDetails Details { get; set; } 

    public virtual ICollection<PlotFolder> PlotFolders { get; set; }

    public string ProjectFieldsOrdering { get; set; }

    Project IProjectEntity.Project => this;

    public virtual ICollection<ProjectFeeSetting>  ProjectFeeSettings { get; set; }
    public virtual ICollection<PaymentType> PaymentTypes { get; set; }

    public DateTime CharacterTreeModifiedAt { get; set; }

    public virtual ICollection<ProjectPlugin> ProjectPlugins { get; set; }

    #region helper properties
    public IEnumerable<PaymentType> ActivePaymentTypes => PaymentTypes.Where(pt => pt.IsActive);

    public CharacterGroup RootGroup => CharacterGroups.Single(g => g.IsRoot);
    #endregion

    public virtual ICollection<GameReport2DTemplate> GameReport2DTemplates { get; set; }
  }

  public class ProjectDetails
  {
    [Key]
    public int ProjectId { get; set; }
    public MarkdownString ClaimApplyRules { get; set; } = new MarkdownString();
    public MarkdownString ProjectAnnounce { get; set; } = new MarkdownString();

    public bool EnableManyCharacters { get; set; }
    public bool PublishPlot { get; set; }
    public int? AllrpgId { get; set; }

    public bool FinanceWarnOnOverPayment { get; set; } = true;
    public bool EnableCheckInModule { get; set; } = false;
    public bool CheckInProgress { get; set; } = false;
    public bool AllowSecondRoles { get; set; } = false;
  }

  // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global used by Entity Framework
  public class ProjectFeeSetting : IValidatableObject
  {
    public int ProjectFeeSettingId { get; set; }
    public int ProjectId { get; set; }
    public virtual Project Project { get; set; }
    public int Fee { get; set; }
    public DateTime StartDate { get; set; }

    #region Implementation of IValidatableObject

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
      if (Fee < 0)
      {
        yield return
          new ValidationResult("Fee should be positive.",
            new List<string> { nameof(Fee) });
      }
    }

    #endregion
  }
}
