namespace JoinRpg.Web.XGameApi.Contract.Schedule
{
    /// <summary>
    /// Information about schedule room
    /// </summary>
    public class RoomInfoApi
    {
        /// <summary>
        /// Id of room. Stable, never changes. Ids for different projects can't overlap.
        /// </summary>
        public int RoomId { get; set; }
        /// <summary>
        /// Name of room (plain text)
        /// </summary>
        public string Name { get; set; }
    }
}
