namespace JoinRpg.DataModel
{
  public class ReadCommentWatermark
  {
    public int ReadCommentWatermarkId { get; set; }
    public int ProjectId { get; set; }
    public int ClaimId { get; set; }
    public virtual Claim Claim { get; set; }
    public int UserId { get; set; }
    public virtual User User { get; set; }
    public int CommentId { get; set; }
    public virtual Comment Comment { get; set; }
  }
}
