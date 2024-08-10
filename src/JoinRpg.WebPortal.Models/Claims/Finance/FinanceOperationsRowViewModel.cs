namespace JoinRpg.Web.Models;

public class FinanceOperationsRowViewModel
{
    public string HtmlId { get; set; }

    public bool Visible { get; set; }

    public bool Disabled { get; set; }

    public string Title { get; set; }

    public int? Fee { get; set; }

    public string? Date { get; set; }

    public string FeeHtmlId { get; set; }

    public int? Payment { get; set; }

    public object AdminFunctions { get; set; }
}
