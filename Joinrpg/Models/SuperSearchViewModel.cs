using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Models
{

  public class SuperSearchViewModel
  {
    [Display(Name="Искать"), Required]
    public string SearchRequest { get; set; }
  }
}
