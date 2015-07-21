using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JoinRpg.DataModel;
using Newtonsoft;

// ReSharper disable once CheckNamespace
namespace JoinRpg.Domain
{
  public class CharacterFieldValue
  {
    private string _value;

    public CharacterFieldValue(CharacterFieldsContainter containter, ProjectCharacterField field, string value)
    {
      Containter = containter;
      Field = field;
      Value = value;
    }

    public ProjectCharacterField Field { get; }

    public string Value
    {
      get { return _value; }
      set
      {
        _value = value;
        Containter.Update();
      }
    }

    private CharacterFieldsContainter Containter { get; }
  }
}
