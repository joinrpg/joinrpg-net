using Autofac;
using JoinRpg.Experimental.Plugin.Interfaces;
using JoinRpg.Experimental.Plugin.DeusEx;
using JoinRpg.Experimental.Plugin.HelloWorld;
using JoinRpg.Experimental.Plugin.PlayerIdCard;
using JoinRpg.Experimental.Plugin.SteampunkDetective;

namespace JoinRpg.Web.App_Start
{
    public class PluginsModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterTypes(typeof(DeusExPlugin), typeof(HelloWorldPlugin), typeof(PlayerIdCardPlugin), typeof(DetectivePlugin)).As<IPlugin>();

            base.Load(builder);
        }
    }
}
