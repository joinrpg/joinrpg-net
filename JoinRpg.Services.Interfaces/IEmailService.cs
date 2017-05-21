using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;

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
    Task Email(CharacterFieldsChangedEmail characterFiledsEmail);
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
    public IReadOnlyCollection<FieldWithValue> UpdatedFields { get; set; } = new List<FieldWithValue>();
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
    public IReadOnlyCollection<FieldWithValue> UpdatedFields { get; set; } = new List<FieldWithValue>();
    public IFieldContainter FiledsContainer => Claim;
  }

  public class CharacterFieldsChangedEmail : EmailModelBase, IEmailWithUpdatedFieldsInfo
  {
    public IReadOnlyCollection<FieldWithValue> UpdatedFields { get; set; } = new List<FieldWithValue>();
    public IFieldContainter FiledsContainer => Character;
    [NotNull]
    public Character Character { get; set; }
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
    IReadOnlyCollection<FieldWithValue> UpdatedFields { get; }

    IFieldContainter FiledsContainer { get; }
  }
}