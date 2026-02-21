using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using JoinRpg.Helpers;

namespace JoinRpg.DataModel;

[ComplexType]
public class IntList
{
    public int[] _parentCharacterGroupIds = [];

    [EditorBrowsable(EditorBrowsableState.Never)]
    public string ListIds
    {
        get;
        set
        {
            field = value;
            _parentCharacterGroupIds = [.. value?.ParseToIntList() ?? []];
        }
    }
}
