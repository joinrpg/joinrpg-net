using JoinRpg.DataModel;

namespace JoinRpg.Services.Interfaces.Notification;

public class PlotElementEmail : EmailModelBase
{

    public required PlotElement PlotElement { get; set; }

    public required IEnumerable<Claim> Claims { get; set; }

}

public class PublishPlotElementEmail : PlotElementEmail
{

}
