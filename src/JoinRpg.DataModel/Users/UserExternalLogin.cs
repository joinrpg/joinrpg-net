using JetBrains.Annotations;

namespace JoinRpg.DataModel
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class UserExternalLogin
    {
        public int UserExternalLoginId { get; set; }
        public int UserId { get; set; }
        [NotNull]
        public virtual User User { get; set; }
        public string Provider { get; set; }
        public string Key { get; set; }
    }
}
