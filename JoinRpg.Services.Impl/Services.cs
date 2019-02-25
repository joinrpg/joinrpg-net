using System;
using System.Collections.Generic;
using JoinRpg.Services.Impl.Search;

namespace JoinRpg.Services.Impl
{
    public static class Services
    {
        public static IEnumerable<Type> GetTypes()
        {
            yield return typeof(ProjectService);
            yield return typeof(ClaimServiceImpl);
            yield return typeof(SearchServiceImpl);
            yield return typeof(PlotServiceImpl);
            yield return typeof(UserServiceImpl);
            yield return typeof(FinanceOperationsImpl);
            yield return typeof(ForumServiceImpl);
            yield return typeof(FieldSetupServiceImpl);
            yield return typeof(FieldDefaultValueGenerator);
            yield return typeof(AccommodationInviteServiceImpl);
            yield return typeof(AccommodationServiceImpl);
            yield return typeof(AntiSpamServiceImpl);
        }
    }
}
