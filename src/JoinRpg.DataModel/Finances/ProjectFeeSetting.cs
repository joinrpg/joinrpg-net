using System.ComponentModel.DataAnnotations;

namespace JoinRpg.DataModel
{
    public class ProjectFeeSetting
    {
        public int ProjectFeeSettingId { get; set; }
        public int ProjectId { get; set; }
        public virtual Project Project { get; set; }
        [Range(0, int.MaxValue, ErrorMessage = "Fee should be positive.")]
        public int Fee { get; set; }
        [Range(0, int.MaxValue, ErrorMessage = "Fee should be positive.")]
        public int? PreferentialFee { get; set; }
        public DateTime StartDate { get; set; }
    }
}
