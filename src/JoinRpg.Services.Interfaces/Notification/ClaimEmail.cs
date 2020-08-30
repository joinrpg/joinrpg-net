using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Domain.CharacterFields;

namespace JoinRpg.Services.Interfaces.Notification
{




    public class AddCommentEmail : ClaimEmailModel
    {
    }

    public class NewClaimEmail : ClaimEmailModel, IEmailWithUpdatedFieldsInfo
    {
        public IReadOnlyCollection<FieldWithPreviousAndNewValue> UpdatedFields { get; set; } =
            new List<FieldWithPreviousAndNewValue>();

        public IFieldContainter FieldsContainer => Claim;

        public IReadOnlyDictionary<string, PreviousAndNewValue> OtherChangedAttributes { get; } =
            new Dictionary<string, PreviousAndNewValue>();
    }

    public class ApproveByMasterEmail : ClaimEmailModel
    {
    }

    public class CheckedInEmal : ClaimEmailModel
    {
    }

    public class SecondRoleEmail : ClaimEmailModel
    {
    }

    public class DeclineByMasterEmail : ClaimEmailModel
    {
    }

    public class OnHoldByMasterEmail : ClaimEmailModel
    {

    }

    public class FieldsChangedEmail : EmailModelBase, IEmailWithUpdatedFieldsInfo
    {
        public IReadOnlyCollection<FieldWithPreviousAndNewValue> UpdatedFields { get; }

        public IFieldContainter FieldsContainer { get;}
        public ILinkable Linkable { get; }

        [NotNull]
        public IReadOnlyDictionary<string, PreviousAndNewValue> OtherChangedAttributes { get; }

        [CanBeNull]
        public Claim Claim { get; }

        [NotNull]
        public string Name { get; }

        //Is character is null, Claim is not null and vice versa. (restricted by constructors).
        public bool IsCharacterMail { get; }

        public FieldsChangedEmail(
            Claim claim,
            User initiator,
            ICollection<User> recipients,
            IReadOnlyCollection<FieldWithPreviousAndNewValue> updatedFields,
            IReadOnlyDictionary<string, PreviousAndNewValue>? otherChangedAttributes = null)
            : this(null, claim, initiator, recipients, updatedFields, otherChangedAttributes)
        {
        }

        public FieldsChangedEmail(
            Character character,
            User initiator,
            ICollection<User> recipients,
            IReadOnlyCollection<FieldWithPreviousAndNewValue> updatedFields,
            [CanBeNull]
            IReadOnlyDictionary<string, PreviousAndNewValue> otherChangedAttributes)
            : this(character, null, initiator, recipients, updatedFields, otherChangedAttributes)
        {
        }

        private FieldsChangedEmail(
            Character? character,
            Claim? claim,
            User initiator,
            ICollection<User> recipients,
            [NotNull]
            IReadOnlyCollection<FieldWithPreviousAndNewValue> updatedFields,
            [CanBeNull]
            IReadOnlyDictionary<string, PreviousAndNewValue>? otherChangedAttributes)
        {
            if (character != null && claim != null)
            {
                throw new ArgumentException(
                    $"Both {nameof(character)} and {nameof(claim)} were provided");
            }
            else if (claim != null)
            {
                FieldsContainer = claim;
                Linkable = claim;
                ProjectName = claim.Project.ProjectName;
                Claim = claim;
                Name = claim.Name;
            }
            else if (character != null)
            {
                FieldsContainer = character;
                Linkable = character;
                ProjectName = character.Project.ProjectName;
                Claim = character.ApprovedClaim;
                Name = character.CharacterName;
                IsCharacterMail = true;
            }
            else
            {
                throw new ArgumentException(
                   $"Neither  {nameof(character)} nor {nameof(claim)} were provided");
            }

            Character = character;
            Initiator = initiator;
            Text = new MarkdownString();
            Recipients = recipients;
            UpdatedFields = updatedFields ?? throw new ArgumentNullException(nameof(updatedFields));
            OtherChangedAttributes = otherChangedAttributes ?? new Dictionary<string, PreviousAndNewValue>();
        }
    }

    public class RestoreByMasterEmail : ClaimEmailModel
    {
    }

    public class MoveByMasterEmail : ClaimEmailModel
    {
    }

    public class ChangeResponsibleMasterEmail : ClaimEmailModel
    {
    }

    public class DeclineByPlayerEmail : ClaimEmailModel
    {
    }

    public class FinanceOperationEmail : ClaimEmailModel
    {
        public int FeeChange { get; set; }
        public int Money { get; set; }
    }

    public class ClaimEmailModel : EmailModelBase
    {
        public ParcipantType InitiatorType { get; set; }
        public Claim Claim { get; set; }
        public CommentExtraAction? CommentExtraAction { get; set; }
    }


    public enum ParcipantType
    {
        Nobody,
        Master,
        Player,
    }
}
