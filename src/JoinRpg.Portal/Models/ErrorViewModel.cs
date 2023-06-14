namespace JoinRpg.Portal.Models;

public class ErrorViewModel
{
    public required string RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    public required string Path { get; set; }

    public required string AspNetTrace { get; set; }
}
