using System;
using System.Collections.Generic;

namespace JoinRpg.WebPortal.Managers
{
    public static class Registration
    {
        public static IEnumerable<Type> GetTypes()
        {
            yield return typeof(ProjectListManager);
        }
    }
}
