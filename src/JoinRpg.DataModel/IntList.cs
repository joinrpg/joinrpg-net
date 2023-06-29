using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using JoinRpg.Helpers;

namespace JoinRpg.DataModel;

[ComplexType]
public class IntList
{
    private string _internalData;
    public int[] _parentCharacterGroupIds = Array.Empty<int>();

    [EditorBrowsable(EditorBrowsableState.Never)]
    public string ListIds
    {
        get => _internalData;
        set
        {
            _internalData = value;
            _parentCharacterGroupIds = value.ToIntList();
        }
    }
}
