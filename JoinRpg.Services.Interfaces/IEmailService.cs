using System.Collections.Generic;
using System.Threading.Tasks;
using JoinRpg.DataModel;

namespace JoinRpg.Services.Interfaces
{
  public interface IEmailService
  {
    Task Email(AddCommentEmail model);
    Task Email(ApproveByMasterEmail createClaimEmail);
    Task Email(DeclineByMasterEmail createClaimEmail);
    Task Email(DeclineByPlayerEmail createClaimEmail);
    Task Email(NewClaimEmail createClaimEmail);
  }

  public class AddCommentEmail : ClaimEmailBase
  {
  }

  public class NewClaimEmail : ClaimEmailBase
  {
  }

  public class ApproveByMasterEmail : ClaimEmailBase
  {
  }

  public class DeclineByMasterEmail : ClaimEmailBase
  {
  }

  public class DeclineByPlayerEmail : ClaimEmailBase
  {
  }

  public class ClaimEmailBase
  {
    public ICollection<User> Recepients { get; set; }
    public string ProjectName { get; set; }
    public ParcipantType InitiatorType { get; set; }
    public Claim Claim { get; set; }
    public User Initiator { get; set; }
    public MarkdownString Text { get; set; }
  }

  public enum ParcipantType 
  {
    Nobody,
    Master,
    Player
  }
}