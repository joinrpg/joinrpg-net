using System;
using System.Collections.Generic;
using System.Linq;

namespace JoinRpg.Web.XGameApi.Contract
{
    /// <summary>
    /// Full character info
    /// </summary>
    public class CharacterInfo
    {
        /// <summary>
        /// Id of character. Globallly unique between all projects.
        /// </summary>
        public int CharacterId { get; set; }

        /// <summary>
        /// Last modified (UTC)
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Active /deleted
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// True, if character in game (player checked in and not checked out)
        /// </summary>
        public bool InGame { get; set; }

        //Need to duplicate here because of swagger limitation
        /// <summary>
        /// Has player or not.
        /// 0 = Has Player,
        /// 1 = Discussed (has some some claims, but nothing approved)
        /// 2 = No Active Claims
        /// 3 = NPC
        /// </summary>
        public CharacterBusyStatus BusyStatus { get; set; }

        /// <summary>
        /// Groups that character part of (directly)
        /// </summary>
        public IOrderedEnumerable<GroupHeader> Groups { get; set; }

        /// <summary>
        /// Groups that character part of 
        /// </summary>
        public IOrderedEnumerable<GroupHeader> AllGroups { get; set; }

        /// <summary>
        /// Field values
        /// </summary>
        public IEnumerable<FieldValue> Fields { get; set; }

        /// <summary>
        /// This is legacy field. Please look into PlayerInfo
        /// </summary>
        [Obsolete]
        public int? PlayerUserId { get; set; }

        /// <summary>
        /// Character name
        /// </summary>
        public string CharacterName { get; set; }

        /// <summary>
        /// This is legacy field. Please use into Fields to get character description.
        /// In future will be removed.
        /// </summary>
        [Obsolete]
        public string CharacterDescription { get; set; }


        /// <summary>
        /// Only set if player present (BusyStatus = HasPlayer)
        /// </summary>
        public CharacterPlayerInfo PlayerInfo { get; set; }
    }

    /// <summary>
    /// Info about player
    /// </summary>
    public record CharacterPlayerInfo(int PlayerUserId, bool PaidInFull, PlayerContacts PlayerContacts)
    {
        /// <summary>
        /// true — claim fee paid in full
        /// false — claim fee not paid in full
        /// null 
        /// </summary>
        public bool PaidInFull { get; set; } = PaidInFull;

        /// <summary>
        /// Player user id
        /// </summary>
        public int PlayerUserId { get; set; } = PlayerUserId;

        /// <summary>
        /// Player contacts
        /// </summary>
        public PlayerContacts PlayerContacts { get; set; } = PlayerContacts;
    }

    /// <summary>
    /// Has player or not.
    /// </summary>
    public enum CharacterBusyStatus
    {
        /// <summary>
        /// Has player
        /// </summary>
        HasPlayer,

        /// <summary>
        /// Has some claims, but nothing approved
        /// </summary>
        Discussed,

        /// <summary>
        /// No actve claims
        /// </summary>
        NoClaims,

        /// <summary>
        /// NPC should not have any claims
        /// </summary>
        Npc,
    }
}
