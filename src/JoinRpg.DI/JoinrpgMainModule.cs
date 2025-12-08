using Autofac;

namespace JoinRpg.DI;

public class JoinrpgMainModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        _ = builder.RegisterTypes(Services.Impl.Services.GetTypes().ToArray()).AsImplementedInterfaces().AsSelf();

        base.Load(builder);
    }
}
