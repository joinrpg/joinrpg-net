namespace JoinRpg.Web.XGameApi.Contract
{
    /// <summary>
    /// Project fields
    /// </summary>
    public class ProjectFieldsMetadata
    {
        /// <summary>
        /// Id
        /// </summary>
        public int ProjectId { get; set; }
        /// <summary>
        /// Name
        /// </summary>
        public string ProjectName { get; set; }
        /// <summary>
        /// Fields
        /// </summary>
        public IEnumerable<ProjectFieldInfo> Fields { get; set; }
    }
}
