using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using JetBrains.Annotations;
using JoinRpg.Helpers;

namespace JoinRpg.DataModel
{
  [ComplexType]
  public class IntList
  {
    private string _internalData;
    internal int[] _parentCharacterGroupIds = {};

    [EditorBrowsable(EditorBrowsableState.Never), UsedImplicitly]
    public string ListIds
    {
      get { return _internalData; }
      set
      {
        _internalData = value;
        _parentCharacterGroupIds = value.ToIntList();
      }
    }
  }
}