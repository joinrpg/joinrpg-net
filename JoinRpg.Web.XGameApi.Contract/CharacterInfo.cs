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
        /// Id
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

        /// <summary>
        /// Has player or not
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
        /// Player user id
        /// </summary>
        public int? PlayerUserId { get; set; }

        /// <summary>
        /// Character name
        /// </summary>
        public string CharacterName { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        public string CharacterDescription { get; set; }
    }

    /// <summary>
    /// Has player or not
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
