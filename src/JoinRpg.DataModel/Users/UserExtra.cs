using System;

namespace JoinRpg.DataModel
{
    public class UserExtra
    {
        public int UserId { get; set; }
        public byte GenderByte { get; set; }

        public Gender Gender
        {
            get => (Gender)GenderByte;
            set => GenderByte = (byte)value;
        }

        public string PhoneNumber { get; set; }
        public string Skype { get; set; }

        public string? Vk { get; set; }
        public bool VkVerified { get; set; }
        public string? Livejournal { get; set; }

        public string Nicknames { get; set; }

        public string GroupNames { get; set; }

        public string? Telegram { get; set; }

        public DateTime? BirthDate { get; set; }

        public ContactsAccessType SocialNetworksAccess { get; set; }

        public override string ToString() => $"UserExtra(UserId: {UserId}, Gender: {Gender}, PhoneNumber: {PhoneNumber}, Nicknames: {Nicknames}, GroupNames: {GroupNames}, BirthDate: {BirthDate}, Telegram: {Telegram})";
    }
}
