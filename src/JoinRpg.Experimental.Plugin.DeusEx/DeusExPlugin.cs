using System.Collections.Generic;
using JoinRpg.Experimental.Plugin.Interfaces;

namespace JoinRpg.Experimental.Plugin.DeusEx
{
    public class DeusExPlugin : IPlugin
    {
        public IEnumerable<PluginOperationMetadata> GetOperations()
        {
            yield return new PluginOperationMetadata("GeneratePIN", typeof(GeneratePinOperation),
              "Генерация PIN-кода", false, () => new GeneratePinOperation(),
              nameof(GeneratePinOperation));
        }

        public string GetName() => "DeusEx";

        public string GetDescripton() => "Различный функционал для поддержки игры Deus Ex";
    }
}
