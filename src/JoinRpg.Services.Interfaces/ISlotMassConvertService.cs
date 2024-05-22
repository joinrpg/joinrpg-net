using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Services.Interfaces;
public interface ISlotMassConvertService
{
    Task MassConvert(ProjectIdentification projectId, bool considerClosed);
}
