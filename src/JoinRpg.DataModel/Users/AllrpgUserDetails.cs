namespace JoinRpg.DataModel
{
    public class AllrpgUserDetails
    {
        public int UserId { get; set; }
        public int? Sid { get; set; }
        public string JsonProfile { get; set; }

        [Obsolete("Not used anymore")]
        public bool PreventAllrpgPassword { get; set; }

        public override string ToString() => $"AllrpgUser(UserId: {UserId}, Sid: {Sid}, JsonProfile: {JsonProfile}";
    }
}
