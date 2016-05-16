﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.DataModel;

namespace JoinRpg.Data.Interfaces
{
  public interface IPlotRepository : IDisposable
  {

    Task<List<PlotFolder>> GetPlots(int project);
    Task<List<PlotFolder>> GetPlotsWithTargets(int project);

    Task<PlotFolder> GetPlotFolderAsync(int projectId, int plotFolderId);
    Task<IReadOnlyCollection<PlotElement>> GetPlotsForCharacter ([NotNull] Character character);
    Task<IReadOnlyCollection<PlotFolder>> GetPlotsWithTargetAndText(int projectid);
  }
}
