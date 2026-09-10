namespace JoinRpg.IdPortal.OAuthServer;

/// <summary>
/// Query string contract between <c>connect/authorize</c> and the consent page (ADR012 §4):
/// the consent page redirects back to <c>connect/authorize</c> carrying the user's decision.
/// </summary>
public static class OAuthConsent
{
    public const string ConsentParameter = "consent";
    public const string ProjectsParameter = "projects";
    public const string Granted = "granted";
    public const string Denied = "denied";

    /// <summary>
    /// Claim type carrying the project ids the user granted access to. Access-token-only.
    /// </summary>
    public const string ProjectsClaimType = "projects";

    public static string FormatProjectIds(IEnumerable<int> projectIds) => string.Join(",", projectIds);

    public static IReadOnlyList<int> ParseProjectIds(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return [];
        }

        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(int.Parse)
            .ToList();
    }
}
