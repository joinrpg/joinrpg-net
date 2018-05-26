using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Helpers;

namespace JoinRpg.Web.Models.Reports
{
  public class Report2DResultViewModel
  {
    public Report2DResultViewModel(GameReport2DTemplate template)
    {
      Name = template.GameReport2DTemplateName;
      var rowsSet = GenerateSet(template, template.FirstCharacterGroup);

      var columnsSet = GenerateSet(template, template.SecondCharacterGroup);

      Rows = GetChildGroups(rowsSet);
      Columns = GetChildGroups(columnsSet);

      Values = rowsSet.SelectMany(row => columnsSet.Select(column => new {row, column}))
        .ToDictionary(
          x => new KeyValuePair<int, int>(x.row.Key, x.column.Key),
          x => new CellValue(x.row.Value.Characters.Intersect(x.column.Value.Characters).ToList())
        );
    }

    private static IReadOnlyCollection<Character> GetFlatCharacters(CharacterGroup group)
    {
      var flatChilds = group.FlatTree(model => model.ChildGroups);

      var flatCharacters = flatChilds.SelectMany(c => c.Characters).Distinct().ToList();
      return flatCharacters;
    }

    private static Dictionary<int, GroupChars> GenerateSet(GameReport2DTemplate template, CharacterGroup group)
    {
      
      var rowsSet = group.ChildGroups
        .Where(g => !g.IsSpecial || !g.ChildGroups.Any())
        .ToDictionary(
        g => g.CharacterGroupId,
        g => new GroupChars {Name = g.CharacterGroupName, Characters =GetFlatCharacters(g)});

      rowsSet.Add(-1,
        new GroupChars
        {
          Name = null,
          Characters = template.Project.Characters
            .Except(rowsSet.SelectMany(r => r.Value.Characters)).ToList(),
        });
      return rowsSet;
    }

    private class GroupChars
    {
      public string Name { get; set; }
      public IReadOnlyCollection<Character> Characters { get; set; }
    }

    private static Dictionary<int, string> GetChildGroups(Dictionary<int, GroupChars> gr)
    {
      return gr.OrderBy(g => g.Key).ToDictionary(
        group => group.Key,
        group => group.Value.Name);
    }

    public string Name { get; }
    public IDictionary<KeyValuePair<int, int>, CellValue> Values { get;  }
    public IDictionary<int, string> Rows { get; }
    public IDictionary<int, string> Columns { get; }
  }

  public class CellValue
  {
    public CellValue(List<Character> characters)
    {
      Count = characters.Count;
      CountWithClaims = characters.Count(c => c.ApprovedClaim != null);
    }

    public int Count { get; }
    public int CountWithClaims { get; }
  }
}