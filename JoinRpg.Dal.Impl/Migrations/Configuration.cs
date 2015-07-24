using System;
using System.Data.Entity.Migrations;
using JetBrains.Annotations;
using JoinRpg.DataModel;

namespace JoinRpg.Dal.Impl.Migrations
{
  [UsedImplicitly]
  internal sealed class Configuration : DbMigrationsConfiguration<MyDbContext>
  {
    public Configuration()
    {
      AutomaticMigrationsEnabled = true;
    }

    protected override void Seed(MyDbContext context)
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

      context.Set<ProjectAcl>().AddOrUpdate(pa => pa.ProjectAclId,
        new ProjectAcl()
        {
          ProjectAclId = 1,
          UserId = 1,
          CanChangeFields = true,
          ProjectId = 1
        });

      context.Set<CharacterGroup>().AddOrUpdate(cg => cg.CharacterGroupId, new CharacterGroup()
      {
        CharacterGroupId = 1,
        ProjectId = 1,
        IsRoot = true,
        CharacterGroupName = "Все персонажи на игре",
        IsActive = true
      });
    }
  }
}