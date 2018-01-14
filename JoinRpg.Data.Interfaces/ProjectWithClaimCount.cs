namespace JoinRpg.Data.Interfaces
{
    public class ProjectWithClaimCount
    {
        public int ProjectId { get; set; }
        public bool Active { get; set; }
        public bool PublishPlot { get; set; }
        public string ProjectName { get; set; }
        public bool IsAcceptingClaims { get; set; }
        public int ActiveClaimsCount { get; set; }
        public bool HasMyClaims { get; set; }
        public bool HasMasterAccess { get; set; }
    }
}
