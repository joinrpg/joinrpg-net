using JoinRpg.DataModel;
using Microsoft.AspNet.Identity;

namespace JoinRpg.Dal.Impl.Migrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<JoinRpg.Dal.Impl.MyDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(JoinRpg.Dal.Impl.MyDbContext context)
        {
         
          context.UserSet.AddOrUpdate(userinfo => userinfo.UserId, new User
          {
            BornName = "Leonid",
            SurName = "Tsarev",
            UserName = "leo@bastilia.ru",
            Email = "leo@bastilia.ru",
            PasswordHash = "AMDQvWY72kqME9N6ZQRnQuo/Vd94C5iBN5ni8lj3ELLIzR7d0brqq5xBt69CQxmjdA==",
            UserId = 1
          });

          context.ProjectsSet.AddOrUpdate(project => project.ProjectId,
            new Project()
            {
              ProjectId = 1,
              Active = true,
              CreatorUserId = 1,
              ProjectName = "TestActive",
              CreatedDate = new DateTime(1970, 1, 1),
            });

          var p = context.ProjectsSet.Find(1);
          if (p.ProjectAcls.All(pa => pa.UserId != 1))
          {
            context.Set<ProjectAcl>().Add(new ProjectAcl()
            {
              UserId = 1,
              CanChangeFields = true,
              ProjectId = 1
            });
          }

          if (p.CharacterGroups.All(cg => !cg.IsRoot))
          {
            context.Set<CharacterGroup>().Add(new CharacterGroup()
            {
              ProjectId = 1, IsRoot = true, CharacterGroupName = "Все персонажи на игре"
            });
          }
        }
    }
}
