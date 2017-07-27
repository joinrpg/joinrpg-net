using System;
using JetBrains.Annotations;
using JoinRpg.Domain.CharacterFields;
using JoinRpg.Services.Impl.Search;
using JoinRpg.Services.Interfaces;
using Microsoft.Practices.Unity;

namespace JoinRpg.Services.Impl
{
  public static class Services
  {
    public static void Register([NotNull] IUnityContainer container)
    {
      if (container == null) throw new ArgumentNullException(nameof(container));

      container.RegisterType<IProjectService, ProjectService>();
      container.RegisterType<IClaimService, ClaimServiceImpl>();
      container.RegisterType<ISearchService, SearchServiceImpl>();
      container.RegisterType<IPlotService, PlotServiceImpl>();
      container.RegisterType<IUserService, UserServiceImpl>();
      container.RegisterType<IFinanceService, FinanceOperationsImpl>();
      container.RegisterType<IForumService, ForumServiceImpl>();
      container.RegisterType<IFieldSetupService, FieldSetupServiceImpl>();
      container.RegisterType<IFieldDefaultValueGenerator, FieldDefaultValueGenerator>();
    }
  }
}
