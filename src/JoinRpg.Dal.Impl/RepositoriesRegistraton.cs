using JoinRpg.Dal.Impl.Repositories;

namespace JoinRpg.Dal.Impl
{
    public static class RepositoriesRegistraton
    {
        public static IEnumerable<Type> GetTypes()
        {
            yield return typeof(ProjectRepository);
            yield return typeof(UserInfoRepository);
            yield return typeof(ClaimsRepositoryImpl);
            yield return typeof(PlotRepositoryImpl);
            yield return typeof(ForumRepositoryImpl);
            yield return typeof(CharacterRepositoryImpl);
            yield return typeof(AccommodationRepositoryImpl);
            yield return typeof(AccommodationRequestRepositoryImpl);
            yield return typeof(AccommodationInviteRepositoryImpl);
            yield return typeof(FinanceReportRepositoryImpl);
        }
    }
}
