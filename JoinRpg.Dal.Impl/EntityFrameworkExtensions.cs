using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.Infrastructure.Annotations;
using System.Data.Entity.ModelConfiguration.Configuration;

namespace JoinRpg.Dal.Impl
{
  internal static class EntityFrameworkExtensions
  {
    public static void IsUnique(this PrimitivePropertyConfiguration property)
    {
      property
        .HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new IndexAttribute() {IsUnique = true}));
    }
  }
}