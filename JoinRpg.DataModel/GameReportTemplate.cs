using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoinRpg.DataModel
{
  public class GameReport2DTemplate
  {
    public int GameReport2DTemplateId { get; set; }

    public string GameReport2DTemplateName { get; set; }

    public int ProjectId { get; set; }
    public virtual Project Project { get; set; }

    public int FirstCharacterGroupId { get; set; }
    [ForeignKey(nameof(FirstCharacterGroupId))]
    public CharacterGroup FirstCharacterGroup { get; set; }

    public int SecondCharacterGroupId { get; set; }
    [ForeignKey(nameof(SecondCharacterGroupId))]
    public CharacterGroup SecondCharacterGroup { get; set; }

    public DateTime CreatedAt { get; set; }
    [ForeignKey(nameof(CreatedById))]
    public virtual User CreatedBy { get; set; }
    public int CreatedById { get; set; }

    public DateTime UpdatedAt { get; set; }
    [ForeignKey(nameof(UpdatedById))]
    public virtual User UpdatedBy { get; set; }
    public int UpdatedById { get; set; }
  }
}
