using JoinRpg.Helpers;
using JoinRpg.PrimitiveTypes.Access;

namespace Joinrpg.Web.Identity;
public class PermissionEncoder
{
    private static readonly int listSize = Enum.GetValues<Permission>().Length;
    public static string Encode(Permission[] permission)
    {
        return permission.Select(x => ((int)x).ToString()).JoinStrings(",");
    }

    public static Permission[] Decode(ReadOnlySpan<char> encodedPermissions)
    {
        var list = new List<Permission>(listSize);
        foreach (var item in encodedPermissions.Split(","))
        {
            var i = int.Parse(encodedPermissions[item].Trim());
            list.Add((Permission)i);
        }
        return [.. list];
    }

    public static bool HasPermission(ReadOnlySpan<char> encodedPermissions, Permission permission)
    {
        foreach (var item in encodedPermissions.Split(","))
        {
            var i = int.Parse(encodedPermissions[item].Trim());
            if ((Permission)i == permission)
            {
                return true;
            }
        }
        return false;
    }

    public static string GetProjectPermissionClaimName(int projectId) => JoinClaimTypes.ProjectPrefix + projectId;
}
