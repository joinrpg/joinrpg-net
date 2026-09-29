using System.Diagnostics;

namespace JoinRpg.Services.Impl.Accommodation;

public static class AccommodationPropsServiceActivity
{
    public const string ActivitySourceName = nameof(AccommodationPropsService);
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}
