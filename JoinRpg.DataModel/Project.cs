using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel.Finances;
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

        public virtual ICollection<CharacterGroup> CharacterGroups { get; set; }

        public virtual ICollection<Character> Characters { get; set; }

        public virtual ICollection<Claim> Claims { get; set; }

        public virtual ICollection<FinanceOperation> FinanceOperations { get; set; }

        [NotNull] public virtual ProjectDetails Details { get; set; }

        public virtual ICollection<PlotFolder> PlotFolders { get; set; }

        Project IProjectEntity.Project => this;

        public virtual ICollection<ProjectFeeSetting> ProjectFeeSettings { get; set; }
        public virtual ICollection<PaymentType> PaymentTypes { get; set; }

        public DateTime CharacterTreeModifiedAt { get; set; }

        public virtual ICollection<ProjectPlugin> ProjectPlugins { get; set; }

        #region helper properties

        public IEnumerable<PaymentType> ActivePaymentTypes => PaymentTypes.Where(pt => pt.IsActive);

        public CharacterGroup RootGroup => CharacterGroups.Single(g => g.IsRoot);

        #endregion

        public virtual ICollection<GameReport2DTemplate> GameReport2DTemplates { get; set; }

        public virtual ICollection<MoneyTransfer> MoneyTransfers { get; set; }
    }

    public class ProjectDetails
    {
        [Key] public int ProjectId { get; set; }
        public MarkdownString ClaimApplyRules { get; set; } = new MarkdownString();
        public MarkdownString ProjectAnnounce { get; set; } = new MarkdownString();

        public bool EnableManyCharacters { get; set; }
        public bool PublishPlot { get; set; }

        public bool FinanceWarnOnOverPayment { get; set; } = true;
        public bool PreferentialFeeEnabled { get; set; } = false;
        public MarkdownString PreferentialFeeConditions { get; set; } = new MarkdownString();

        public bool EnableCheckInModule { get; set; } = false;
        public bool CheckInProgress { get; set; } = false;
        public bool AllowSecondRoles { get; set; } = false;
        public bool AutoAcceptClaims { get; set; } = false;
        public bool EnableAccommodation { get; set; } = false;

        /// <summary>
        /// If true, character name is separate property of character, not bound to any field
        /// and could be edited only by master
        /// </summary>
        public bool CharacterNameLegacyMode { get; set; } = true;
        /// <summary>
        /// If legacy mode = true, always null
        /// If legacy mode = false.
        ///     Null = bound to player name
        ///     Other value = bound to that field
        /// </summary>
        [CanBeNull]
        public ProjectField CharacterNameField { get; set; }
        /// <summary>
        ///     Null = no character description
        ///     Other value = bound to that field
        /// </summary>
        [CanBeNull]
        public ProjectField CharacterDescription { get; set; }

        public string FieldsOrdering { get; set; }
    }
}
