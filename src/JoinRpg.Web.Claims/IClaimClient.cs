using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Web.Claims;

public interface IClaimOperationClient
{
    Task AllowSensitiveData(ProjectIdentification projectId);

}
