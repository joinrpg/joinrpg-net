using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.CharacterFields;

namespace JoinRpg.Services.Interfaces
{
  public interface IEmailService
  {
    Task Email(AddCommentEmail model);
    Task Email(ApproveByMasterEmail createClaimEmail);
    Task Email(DeclineByMasterEmail createClaimEmail);
    Task Email(DeclineByPlayerEmail createClaimEmail);
    Task Email(NewClaimEmail createClaimEmail);
    Task Email(RemindPasswordEmail email);
    Task Email(ConfirmEmail email);
    Task Email(RestoreByMasterEmail createClaimEmail);
    Task Email(MoveByMasterEmail createClaimEmail);
    Task Email(FinanceOperationEmail createClaimEmail);
    Task Email(MassEmailModel model);
    Task Email(ChangeResponsibleMasterEmail createClaimEmail);
    Task Email(OnHoldByMasterEmail createClaimEmail);
    Task Email(ForumEmail model);
    Task Email(ClaimFieldsChangedEmail createClaimEmail);
    Task Email(FieldsChangedEmail filedsEmail);
  }

  public static class EmailTokens
  {
    public const string Name = "%NAME%";
  }

  public class RemindPasswordEmail 
  {
    public string CallbackUrl { get; set; }
    public User Recepient { get; set; }
  }

  public class ConfirmEmail
  {
    public string CallbackUrl
    { get; set; }
    public User Recepient
    { get; set; }
  }

  public class AddCommentEmail : ClaimEmailModel
  {
  }

  public class NewClaimEmail : ClaimEmailModel, IEmailWithUpdatedFieldsInfo
  {
    public IReadOnlyCollection<FieldWithOldAndNewValue> UpdatedFields { get; set; } = new List<FieldWithOldAndNewValue>();
    public IFieldContainter FiledsContainer => Claim;
  }

  public class ApproveByMasterEmail : ClaimEmailModel
  {
  }

  public class DeclineByMasterEmail : ClaimEmailModel
  {
  }

  public class OnHoldByMasterEmail : ClaimEmailModel
  {
    
  }

  public class ClaimFieldsChangedEmail : ClaimEmailModel, IEmailWithUpdatedFieldsInfo
  {
    public IReadOnlyCollection<FieldWithOldAndNewValue> UpdatedFields { get; set; } = new List<FieldWithOldAndNewValue>();
    public IFieldContainter FiledsContainer => Claim;
  }

  public class FieldsChangedEmail : EmailModelBase, IEmailWithUpdatedFieldsInfo
  {
    public IReadOnlyCollection<FieldWithOldAndNewValue> UpdatedFields { get; set; } = new List<FieldWithOldAndNewValue>();
    public IFieldContainter FiledsContainer => (IFieldContainter)Claim ?? Character;
    [CanBeNull]
    public Character Character { get; set; }
    [CanBeNull]
    public Claim Claim { get; set; }
    //Is character is null, Claim is not null and vioce versa. (restricted by constructors).
    public bool IsCharacterMail => Character != null;

    public FieldsChangedEmail(
      Claim claim,
      User initiator,
      ICollection<User> recepients,
      IReadOnlyCollection<FieldWithOldAndNewValue> updatedFields)
      : this(null, claim, initiator, recepients, updatedFields)
    {
    }

    public FieldsChangedEmail(
      Character character,
      User initiator,
      ICollection<User> recepients,
      IReadOnlyCollection<FieldWithOldAndNewValue> updatedFields)
      : this(character, null, initiator, recepients, updatedFields)
    {
    }

    private FieldsChangedEmail(
      Character character,
      Claim claim,
      User initiator,
      ICollection<User> recepients,
      [NotNull] IReadOnlyCollection<FieldWithOldAndNewValue> updatedFields)
    {
      if (updatedFields == null) throw new ArgumentNullException(nameof(updatedFields));
      if (character != null && claim != null)
        throw new ArgumentException($"Both {nameof(character)} and {nameof(claim)} were provided");
      if (character == null && claim == null)
        throw new ArgumentException($"Neither  {nameof(character)} nor {nameof(claim)} were provided");

      Character = character;
      Claim = claim;
      ProjectName = character?.Project.ProjectName ?? claim?.Project.ProjectName;
      Initiator = initiator;
      Text = new MarkdownString();
      Recepients = recepients;
      UpdatedFields = updatedFields;
    }
  }

  public class RestoreByMasterEmail : ClaimEmailModel {}

  public class MoveByMasterEmail : ClaimEmailModel
  {
  }

  public class ChangeResponsibleMasterEmail : ClaimEmailModel
  {
  }

  public class DeclineByPlayerEmail : ClaimEmailModel
  {
  }

  public class ForumEmail : EmailModelBase
  {
    public ForumThread ForumThread { get; set; }
  }

  public class FinanceOperationEmail : ClaimEmailModel
  {
    public int FeeChange { get; set; }
    public int Money { get; set; }
  }

  public class MassEmailModel : EmailModelBase
  {
    public string Subject { get; set; }
  }

  public class ClaimEmailModel : EmailModelBase
  {
    public ParcipantType InitiatorType { get; set; }
    public Claim Claim { get; set; }
    public CommentExtraAction? CommentExtraAction { get; set; }
  }

  public class EmailModelBase
  {
    public string ProjectName { get; set; }
    public User Initiator { get; set; }
    public MarkdownString Text { get; set; }
    public ICollection<User> Recepients { get; set; }
  }

  public enum ParcipantType 
  {
    Nobody,
    Master,
    Player
  }

  /// <summary>
  /// This interface to be implemented by emails taht should include the list of updated felds in them.
  /// </summary>
  public interface IEmailWithUpdatedFieldsInfo
  {
    IReadOnlyCollection<FieldWithOldAndNewValue> UpdatedFields { get; }

    IFieldContainter FiledsContainer { get; }
  }
}