using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.Infrastructure.Annotations;
using System.Data.Entity.ModelConfiguration;
using System.Data.Entity.ModelConfiguration.Configuration;
using System.Linq.Expressions;

namespace JoinRpg.Dal.Impl
{
  internal static class EntityFrameworkExtensions
  {
    public static void IsUnique(this PrimitivePropertyConfiguration property)
    {
      property
        .HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new IndexAttribute() {IsUnique = true}));
    }

    public static void HasIndex(this PrimitivePropertyConfiguration property, string indexName = null, int order = 1)
    {
      indexName = indexName ?? new Guid().ToString();
      property
        .HasColumnAnnotation(IndexAnnotation.AnnotationName,
          new IndexAnnotation(new IndexAttribute(indexName) {IsUnique = true, Order = order}));
    }

    public static void HasIndex(this IEnumerable<PrimitivePropertyConfiguration> properties, string indexName)
    {
      var order = 1;
      foreach (var property in properties)
      {
        property.HasIndex(indexName, order);
        order++;
      }
    }

    public static IReadOnlyCollection<PrimitivePropertyConfiguration> Properties<TEntity, T1, T2>(
      this EntityTypeConfiguration<TEntity> entityTypeConfiguration,
      Expression<Func<TEntity, T1>> p1, 
      Expression<Func<TEntity, T2>> p2) 
      where TEntity : class
      where T1 : struct
      where T2 : struct
    {
      return new[]
      {
        entityTypeConfiguration.Property(p1),
        entityTypeConfiguration.Property(p2),
      };
    }
  }
}