using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Data.Interfaces;
public interface IResponsibleMasterRulesRepository
{
    /// <summary>
    /// TODO here we load CharacterGroups. In future it will be decoupled from Groups.
    /// </summary>
    /// <param name="projectId"></param>
    /// <returns></returns>
    Task<List<CharacterGroup>> GetResponsibleMasterRules(ProjectIdentification projectId);
}
