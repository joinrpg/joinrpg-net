namespace JoinRpg.Web.XGameApi.Contract.Schedule;

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
    /// Description of program item (plain text)
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Id of program item. Stable, never changes. Ids for different projects can't overlap.
    /// </summary>
    public int ProgramItemId { get; set; }

    /// <summary>
    /// Description of program item (HTML).
    /// Html is passed through sanitizer on server side and PROBABLY safe.
    /// But your requirements could vary, so it may be wise to sanitize again.
    /// </summary>
    public string DescriptionHtml { get; set; }

    /// <summary>
    /// Description of program item (Markdown). 
    /// Beware: this is user input and should be treated as untrusted.
    /// If you will render this to HTML, you have to sanitize AFTER rendering.
    /// </summary>
    public string DescriptionMarkdown { get; set; }
}
