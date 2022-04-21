namespace JoinRpg.Portal.Models;

public class ErrorViewModel
{
    public string RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    public string Path { get; internal set; }

    public string AspNetTrace { get; set; }
}
