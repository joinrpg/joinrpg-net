using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models.CommonTypes
{
 
  /// <summary>
  /// Used for annotations and AllowHtml
  /// </summary>
  [MetadataType(typeof(MarkdownViewModel.Metadata))]
  public class MarkdownViewModel : MarkdownString
  {
    internal class Metadata
    {
      [AllowHtml]
      public string Contents{ get; set; }
    }

    public MarkdownViewModel(MarkdownString other) : this (other?.Contents)
    {
      
    }

    public MarkdownViewModel()
    { }

    public MarkdownViewModel(string contents)
    {
      Contents = contents?.Trim();
    }
  }
}
