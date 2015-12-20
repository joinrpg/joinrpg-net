using System.ComponentModel.DataAnnotations;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Domain;

namespace JoinRpg.Web.Models
{
  public class SubscribeSettingsViewModel
  {
    public SubscribeSettingsViewModel() { }

    public SubscribeSettingsViewModel(User user, CharacterGroup @group)
    {
      var direct = user.Subscriptions.SingleOrDefault(s => s.CharacterGroupId == group.CharacterGroupId);
      Init(user, @group, direct);
      ProjectId = group.ProjectId;
      CharacterGroupId = group.CharacterGroupId;
      Name = group.CharacterGroupName;
    }

    public string Name { get; set; }

    public SubscribeSettingsViewModel(User user, Character @group)
    {
      var direct = user.Subscriptions.SingleOrDefault(s => s.CharacterId == group.CharacterId);
      Init(user, @group, direct);
    }

    public SubscribeSettingsViewModel(User user, Claim claim)
    {
      var direct = user.Subscriptions.SingleOrDefault(s => s.ClaimId == claim.ClaimId);
      if (direct != null)
      {
        InitFrom(direct);
      }
      else
      {
        
          var direct2 = user.Subscriptions.SingleOrDefault(s => s.CharacterId == claim.CharacterId || s.CharacterGroupId == claim.CharacterGroupId);
          Init(user, claim.GetTarget(), direct2); 
      }
    }

    private void Init(User user, IWorldObject @group, UserSubscription direct)
    {
      ClaimStatusChangeEnabled = CommentsEnabled = FieldChangeEnabled = MoneyOperationEnabled = true;
      if (direct != null)
      {
        InitFrom(direct);
      }

      foreach (var characterGroup in @group.ParentGroups)
      {
        ParseCharacterGroup(characterGroup, user);
      }

    }

    private void ParseCharacterGroup(CharacterGroup characterGroup, User user)
    {
      var subscribe = user.Subscriptions.SingleOrDefault(s => s.CharacterGroupId == characterGroup.CharacterGroupId);
      if (subscribe != null)
      {
        UpdateFrom(subscribe);
        if (!AnythingEnabled)
        {
          return;
        }
      }
      foreach (var parentGroup in characterGroup.ParentGroups)
      {
        ParseCharacterGroup(parentGroup, user);
        if (!AnythingEnabled)
        {
          return;
        }
      }
    }

    private bool AnythingEnabled => ClaimStatusChangeEnabled || FieldChangeEnabled || CommentsEnabled || MoneyOperationEnabled;

    private void UpdateFrom(UserSubscription subscribe)
    {
      if (subscribe.ClaimStatusChange)
      {
        ClaimStatusChange = true;
        ClaimStatusChangeEnabled = false;
      }
      if (subscribe.FieldChange)
      {
        FieldChange = true;
        FieldChangeEnabled = false;
      }
      if (subscribe.Comments)
      {
        Comments = true;
        CommentsEnabled = false;
      }
      if (subscribe.MoneyOperation)
      {
        MoneyOperation = true;
        MoneyOperationEnabled = false;
      }
    }

    private void InitFrom(UserSubscription direct)
    {
      ClaimStatusChange = direct.ClaimStatusChange;
      Comments = direct.Comments;
      FieldChange = direct.FieldChange;
      MoneyOperation = direct.MoneyOperation;
    }

    [Display(Name = "Подписка на новые заявки/прием/отклонение")]
    public bool ClaimStatusChange { get; set; }

    public bool ClaimStatusChangeEnabled { get; set; }

    [Display(Name = "Подписка на комментарии")]
    public bool Comments { get; set; }

    public bool CommentsEnabled { get; set; }

    [Display(Name = "Подписка на изменение полей персонажа")]
    public bool FieldChange { get; set; }

    public bool FieldChangeEnabled { get; set; }

    [Display(Name = "Подписка на финансовые операции")]
    public bool MoneyOperation { get; set; }

    public bool MoneyOperationEnabled { get; set; }

    public bool MoneyOperationValue => MoneyOperationEnabled && MoneyOperation;

    public bool ClaimStatusChangeValue => ClaimStatusChangeEnabled && ClaimStatusChange;
    public bool CommentsValue => CommentsEnabled && Comments;
    public bool FieldChangeValue => FieldChangeEnabled && FieldChange;

    public int ProjectId { get; set; }
    public int CharacterGroupId { get; set; }
  }
}
