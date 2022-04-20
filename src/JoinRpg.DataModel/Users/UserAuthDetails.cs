using JetBrains.Annotations;

namespace JoinRpg.DataModel
{
    public class UserAuthDetails
    {
        public int UserId { get; set; }

        public bool EmailConfirmed { get; set; }

        public DateTime RegisterDate { get; set; }

        public bool IsAdmin { get; set; }

        [NotNull]
        public string AspNetSecurityStamp { get; set; }

        public override string ToString() => $"UserAuthDetails(UserId: {UserId}, EmailConfirmed: {EmailConfirmed}, RegisterDate: {RegisterDate})";
    }
}
