using Autofac;
using JoinRpg.Domain.CharacterFields;
using JoinRpg.Domain.Problems;

namespace JoinRpg.Domain;
public class JoinRpgDomainModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);

        _ = builder.RegisterType<FieldSaveHelper>().AsSelf().InstancePerLifetimeScope();

        _ = builder.RegisterGeneric(typeof(ProblemValidator<>)).AsImplementedInterfaces();

        _ = builder.RegisterAssemblyTypes(typeof(JoinRpgDomainModule).Assembly).AsClosedTypesOf(typeof(IProblemFilter<>)).SingleInstance();

        _ = builder.RegisterAssemblyTypes(typeof(JoinRpgDomainModule).Assembly).AsClosedTypesOf(typeof(IFieldRelatedProblemFilter<>)).SingleInstance();
    }
}
