using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JoinRpg.Web.Models.Print
{
  public class PrintIndexViewModel
  {
    public PrintIndexViewModel(int projectId, IEnumerable<int> characterIds)
    {
      ProjectId = projectId;
      CharacterIds = characterIds;
    }

    public int ProjectId { get; private set; }
    public IEnumerable<int> CharacterIds { get; private set; }
  }
}