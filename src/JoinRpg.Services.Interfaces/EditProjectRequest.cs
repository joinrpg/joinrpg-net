namespace JoinRpg.Services.Interfaces
{
    public class EditProjectRequest
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string ClaimApplyRules { get; set; }
        public string ProjectAnnounce { get; set; }
        public bool IsAcceptingClaims { get; set; }
        public bool MultipleCharacters { get; set; }
        public bool PublishPlot { get; set; }
        public bool AutoAcceptClaims { get; set; }
        public bool IsAccommodationEnabled { get; set; }
    }
}
