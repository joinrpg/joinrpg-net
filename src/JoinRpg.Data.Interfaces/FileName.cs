using JoinRpg.Helpers;

namespace JoinRpg.Data.Interfaces;

public class JoinRpgEntityNotFoundException : JoinRpgBaseException
{
    public JoinRpgEntityNotFoundException(IEnumerable<int> ids, string typeName) : base($"Can't found entities of type {typeName} by ids {string.Join(", ", ids)}")
    {
    }

    public JoinRpgEntityNotFoundException(int id, string typeName) : base($"Can't found entity of type {typeName} by id {id}")
    {
    }
}
