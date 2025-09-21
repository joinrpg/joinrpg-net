using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Web.Claims;
public interface IClaimClient
{
    Task AllowSensitiveData(ProjectIdentification projectId);
}
