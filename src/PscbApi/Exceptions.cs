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
public class PscbApiRequestException<TRequest> : PscbApiExceptionBase
    where TRequest : class
{
    /// <summary>
    /// Api method url
    /// </summary>
    public string Url { get; }

    /// <summary>
    /// Request data
    /// </summary>
    public TRequest Request { get; }

    /// <summary>
    /// Request signature
    /// </summary>
    public string Signature { get; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="url"></param>
    /// <param name="request"></param>
    /// <param name="signature"></param>
    /// <param name="message"></param>
    public PscbApiRequestException(string url, TRequest request, string signature, string message = null)
        : base($"Invalid request to {url}: {message}")
    {
        Url = url;
        Request = request;
        Signature = signature;
    }
}
