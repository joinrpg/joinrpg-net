using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.Dal.Impl;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl
{
  [UsedImplicitly]
    public class ProjectService : DbServiceImplBase, IProjectService
    {
    public ProjectService(IUnitOfWork unitOfWork) : base(unitOfWork)
      {
        
      }

      public Project AddProject(string projectName, User creator)
      {
        var project = new Project()
        {
          Active = true,
          CreatedDate = DateTime.Now,
          CreatorUserId = creator.UserId,
          ProjectName = projectName,
          CharacterGroups = new List<CharacterGroup>()
          {
            new CharacterGroup() {IsPublic = true, IsRoot = true, CharacterGroupName = "Все персонажи на игре", IsActive = true}
          },
          ProjectAcls = new List<ProjectAcl>()
          {
            ProjectAcl.CreateRootAcl(creator.UserId)
          }
        };
        UnitOfWork.GetDbSet<Project>().Add(project);
        UnitOfWork.SaveChanges();
        return project;
      }

      public void AddCharacterField(ProjectCharacterField field)
      {
        field.IsActive = true;
        CheckField(field);
        UnitOfWork.GetDbSet<ProjectCharacterField>().Add(field);
        UnitOfWork.SaveChanges();
      }

      private static void CheckField(ProjectCharacterField field)
      {
        if (field.IsPublic && !field.CanPlayerView)
        {
          throw new DbEntityValidationException();
        }

        if (string.IsNullOrWhiteSpace(field.FieldName))
        {
          throw new DbEntityValidationException();
        }

        field.FieldName = field.FieldName.Trim();
      }

      public void UpdateCharacterField(int projectId, int fieldId, string name, string fieldHint,
        bool canPlayerEdit, bool canPlayerView, bool isPublic)
      {
        var field = LoadProjectSubEntity<ProjectCharacterField>(projectId, fieldId);
        field.FieldName = name;
        field.FieldHint = fieldHint;
        field.CanPlayerEdit = canPlayerEdit;
        field.CanPlayerView = canPlayerView;
        field.IsPublic = isPublic;

        CheckField(field);

        UnitOfWork.SaveChanges();
      }

    // ReSharper disable once UnusedParameter.Local

    public void DeleteField(int projectCharacterFieldId)
      {
        var field = UnitOfWork.GetDbSet<ProjectCharacterField>().Find(projectCharacterFieldId);
        if (field.WasEverUsed)
        {
          field.IsActive = false;
          
        }
        else
        {
          UnitOfWork.GetDbSet<ProjectCharacterField>().Remove(field);
        }
      UnitOfWork.SaveChanges();
    }

      public void AddCharacterGroup(int projectId, string name, bool isPublic, List<int> parentCharacterGroupIds, string description)
      {
        var characterGroups = ValidateCharacterGroupList(projectId, parentCharacterGroupIds);

        if (string.IsNullOrWhiteSpace(name))
        {
          throw new DbEntityValidationException();
        }

        UnitOfWork.GetDbSet<CharacterGroup>().Add(new CharacterGroup()
        {
          AvaiableDirectSlots = 0,
          CharacterGroupName = name,
          ParentGroups = characterGroups,
          ProjectId = projectId,
          IsRoot = false,
          IsPublic = isPublic,
          IsActive =  true,
          Description = new MarkdownString(description)
        });
        UnitOfWork.SaveChanges();
      }

      private List<CharacterGroup> ValidateCharacterGroupList(int projectId, List<int> parentCharacterGroupIds)
      {
        var characterGroups =
          UnitOfWork.GetDbSet<CharacterGroup>().Where(cg => parentCharacterGroupIds.Contains(cg.CharacterGroupId)).ToList();

        if (characterGroups.Any(g => g.ProjectId != projectId))
        {
          throw new DbEntityValidationException();
        }

        if (characterGroups.Count == 0)
        {
          throw new DbEntityValidationException();
        }
        return characterGroups;
      }

      public void AddCharacter(int projectId, List<int> parentCharacterGroupIds, string name, bool isPublic, bool isAcceptingClaims, string description)
      {
      var characterGroups = ValidateCharacterGroupList(projectId, parentCharacterGroupIds);

      if (string.IsNullOrWhiteSpace(name))
      {
        throw new DbEntityValidationException();
      }

      UnitOfWork.GetDbSet<Character>().Add(new Character()
      {
        CharacterName = name,
        Groups = characterGroups,
        ProjectId = projectId,
        IsPublic = isPublic,
        IsActive = true,
        IsAcceptingClaims = isAcceptingClaims,
        Description = new MarkdownString(description)
      });
      UnitOfWork.SaveChanges();
    }
    }
}
