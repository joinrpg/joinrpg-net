using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoinRpg.Web.Models
{
  public class GameFieldEditViewModel
  {
    public int ProjectId { get; set; }
    public int FieldId { get; set; }

    public string Name { get; set; }

    public bool IsPublic { get; set; }

    public bool CanPlayerView { get; set; }

    public bool CanPlayerEdit { get; set; }

    public string FieldHint { get; set; }
  }
}
