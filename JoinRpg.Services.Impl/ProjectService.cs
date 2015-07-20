using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JoinRpg.Dal.Impl;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl
{
    public class ProjectService : IProjectService
    {
      private readonly IUnitOfWork _unitOfWork;

      public ProjectService(IUnitOfWork unitOfWork)
      {
        _unitOfWork = unitOfWork;
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
            new CharacterGroup() {IsPublic = true, IsRoot = true, CharacterGroupName = "Все персонажи на игре"}
          },
          ProjectAcls = new List<ProjectAcl>()
          {
            ProjectAcl.CreateRootAcl(creator.UserId)
          }
        };
        _unitOfWork.GetDbSet<Project>().Add(project);
        _unitOfWork.SaveChanges();
        return project;
      }

      public void AddCharacterField(ProjectCharacterField field)
      {
        field.IsActive = true;
        CheckField(field);
        _unitOfWork.GetDbSet<ProjectCharacterField>().Add(field);
        _unitOfWork.SaveChanges();
      }

      private static void CheckField(ProjectCharacterField field)
      {
        if (field.IsPublic && !field.CanPlayerView)
        {
          throw new DbEntityValidationException();
        }
      }

      public void UpdateCharacterField(int projectId, int fieldId, string name, string fieldHint,
        bool canPlayerEdit, bool canPlayerView, bool isPublic)
      {
        var field = _unitOfWork.GetDbSet<ProjectCharacterField>().Find(fieldId);
        if (field.ProjectId != projectId)
        {
          throw new DbEntityValidationException();
        }
        field.FieldName = name;
        field.FieldHint = fieldHint;
        field.CanPlayerEdit = canPlayerEdit;
        field.CanPlayerView = canPlayerView;
        field.IsPublic = isPublic;


        CheckField(field);

        _unitOfWork.SaveChanges();
      }

      public void DeleteField(int projectCharacterFieldId)
      {
        var field = _unitOfWork.GetDbSet<ProjectCharacterField>().Find(projectCharacterFieldId);
        if (field.WasEverUsed)
        {
          field.IsActive = false;
          
        }
        else
        {
          _unitOfWork.GetDbSet<ProjectCharacterField>().Remove(field);
        }
      _unitOfWork.SaveChanges();
    }
    }
}
