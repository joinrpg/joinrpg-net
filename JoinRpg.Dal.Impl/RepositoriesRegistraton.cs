using JetBrains.Annotations;
using JoinRpg.Dal.Impl.Repositories;
using JoinRpg.Data.Interfaces;
using Microsoft.Practices.Unity;

namespace JoinRpg.Dal.Impl
{
  public static class RepositoriesRegistraton
  {
    public static void Register([NotNull] IUnityContainer container)
    {
      container.RegisterType<IProjectRepository, ProjectRepository>();
      container.RegisterType<IUserRepository, UserInfoRepository>();
      container.RegisterType<IClaimsRepository, ClaimsRepositoryImpl>();
      container.RegisterType<IPlotRepository, PlotRepositoryImpl>();
      container.RegisterType<IForumRepository, ForumRepositoryImpl>();
      container.RegisterType<ICharacterRepository, CharacterRepositoryImpl>();
      container.RegisterType<IAccommodationRepository, AccommodationRepositoryImpl>();
      container.RegisterType<IAccommodationRequestRepository, AccommodationRequestRepositoryImpl>();
    }
  }
}
