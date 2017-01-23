﻿using System.Collections.Generic;
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
    Task Email(RemindPasswordEmail email);
    Task Email(ConfirmEmail email);
    Task Email(RestoreByMasterEmail createClaimEmail);
    Task Email(MoveByMasterEmail createClaimEmail);
    Task Email(FinanceOperationEmail createClaimEmail);
    Task Email(MassEmailModel model);
    Task Email(ChangeResponsibleMasterEmail createClaimEmail);
    Task Email(OnHoldByMasterEmail createClaimEmail);
    Task Email(ForumEmail model);
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

  public class NewClaimEmail : ClaimEmailModel
  {
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

  public class RestoreByMasterEmail : ClaimEmailModel {}

  public class MoveByMasterEmail : ClaimEmailModel
  {
  }

  public class ChangeResponsibleMasterEmail : ClaimEmailModel
  {
    public User NewMaster { get; set; }
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
}