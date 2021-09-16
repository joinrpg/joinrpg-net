using System;
using System.Collections.Generic;
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

        #region helper properties

        public IEnumerable<PaymentType> ActivePaymentTypes => PaymentTypes.Where(pt => pt.IsActive);

        public CharacterGroup RootGroup => CharacterGroups.Single(g => g.IsRoot);

        #endregion

        public virtual ICollection<GameReport2DTemplate> GameReport2DTemplates { get; set; }

        public virtual ICollection<MoneyTransfer> MoneyTransfers { get; set; }
    }
}
