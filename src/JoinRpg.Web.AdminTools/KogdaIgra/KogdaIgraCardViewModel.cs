namespace JoinRpg.Web.AdminTools.KogdaIgra;
public record class KogdaIgraCardViewModel(
    Uri KogdaIgraUri,
    string Name,
    DateTimeOffset Begin,
    DateTimeOffset End,
    string RegionName,
    string MasterGroupName,
    Uri SiteUri)
{
}
