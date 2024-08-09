namespace PscbApi;

/// <summary>
/// Base class for PSCB API Exceptions
/// </summary>
public class PscbApiExceptionBase : Exception
{
    /// <summary>
    /// No message constructor
    /// </summary>
    public PscbApiExceptionBase() : base() { }

    /// <summary>
    /// Creates exception with message
    /// </summary>
    public PscbApiExceptionBase(string message) : base(message) { }
}


/// <summary>
/// Request exception
/// </summary>
/// <typeparam name="TRequest">Type of request data</typeparam>
/// <remarks>
/// Constructor
/// </remarks>
/// <param name="url"></param>
/// <param name="request"></param>
/// <param name="signature"></param>
/// <param name="message"></param>
public class PscbApiRequestException<TRequest>(string url, TRequest request, string signature, string? message = null)
    : PscbApiExceptionBase($"Invalid request to {url}: {message}")
    where TRequest : class
{
    /// <summary>
    /// Api method url
    /// </summary>
    public string Url { get; } = url;

    /// <summary>
    /// Request data
    /// </summary>
    public TRequest Request { get; } = request;

    /// <summary>
    /// Request signature
    /// </summary>
    public string Signature { get; } = signature;
}
