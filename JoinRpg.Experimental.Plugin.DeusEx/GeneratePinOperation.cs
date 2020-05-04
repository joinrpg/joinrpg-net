using System;
using JoinRpg.Experimental.Plugin.Interfaces;

namespace JoinRpg.Experimental.Plugin.DeusEx
{
    public class GeneratePinOperation : IGenerateFieldOperation
    {
        public void SetConfiguration(IPluginConfiguration pluginConfiguration)
        {

        }

        public string GenerateFieldValue(CharacterInfo character, CharacterFieldInfo fieldInfo)
        {
            var random = new Random(character.CharacterId);
            return random.Next(9999).ToString("D4");
        }
    }
}
