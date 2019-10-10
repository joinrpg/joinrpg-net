using System;
using System.Collections.Generic;

namespace JoinRpg.Web.XGameApi.Contract.Schedule
{
    /// <summary>
    /// Information about program item
    /// </summary>
    public class ProgramItemInfoApi
    {
        /// <summary>
        /// Name of program item (plain text)
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Authors of program item
        /// </summary>
        public IEnumerable<AuthorInfoApi> Authors { get; set; }
        /// <summary>
        /// Time when program item starts (with timezone)
        /// </summary>
        public DateTimeOffset StartTime { get; set; }
        /// <summary>
        /// /// Time where program item ends (with timezone)
        /// </summary>
        public DateTimeOffset EndTime { get; set; }
        /// <summary>
        /// Information about used rooms
        /// </summary>
        public IEnumerable<RoomInfoApi> Rooms { get; set; }
        /// <summary>
        /// Description of program item (HTML)
        /// </summary>
        public string Description { get; set; }
    }
}
