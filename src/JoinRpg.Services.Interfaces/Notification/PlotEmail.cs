using System.Collections.Generic;
using JoinRpg.DataModel;

namespace JoinRpg.Services.Interfaces.Notification
{
    public class PlotElementEmail : EmailModelBase
    {

        public PlotElement PlotElement { get; set; }

        public IEnumerable<Claim> Claims { get; set; }

    }

    public class PublishPlotElementEmail : PlotElementEmail
    {

    }
}
