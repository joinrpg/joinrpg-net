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

        _ = builder.RegisterType<CharacterProblemValidator>().AsImplementedInterfaces();

        _ = builder.RegisterType<ClaimProblemValidator>().AsImplementedInterfaces();

        // ICharacterProblemFilter / IClaimProblemFilter не generic, поэтому AsClosedTypesOf тут
        // не подходит — отбираем реализации явно.
        _ = builder.RegisterAssemblyTypes(typeof(JoinRpgDomainModule).Assembly)
            .AssignableTo<ICharacterProblemFilter>()
            .As<ICharacterProblemFilter>()
            .SingleInstance();

        _ = builder.RegisterAssemblyTypes(typeof(JoinRpgDomainModule).Assembly)
            .AssignableTo<IClaimProblemFilter>()
            .As<IClaimProblemFilter>()
            .SingleInstance();

        _ = builder.RegisterAssemblyTypes(typeof(JoinRpgDomainModule).Assembly).AsClosedTypesOf(typeof(IFieldRelatedProblemFilter<>)).SingleInstance();
    }
}
