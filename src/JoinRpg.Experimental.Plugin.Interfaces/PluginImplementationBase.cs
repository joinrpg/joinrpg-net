using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace JoinRpg.Experimental.Plugin.Interfaces
{
    public abstract class PluginImplementationBase : IPlugin
    {
        private readonly IList<PluginOperationMetadata> _operations = new List<PluginOperationMetadata>();

        protected void Register<T>([NotNull] string name, [NotNull] string description = "", bool allowPlayerAccess = false, string fieldMappng = null)
          where T : IPluginOperation, new()
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (description == null)
            {
                throw new ArgumentNullException(nameof(description));
            }

            var fieldOperation = typeof(IFieldOperation).IsAssignableFrom(typeof(T));
            if (fieldMappng == null && fieldOperation)
            {
                throw new PluginRegistrationIncorrectException($"Field mapping should be defined for {nameof(IFieldOperation)}");
            }

            if (fieldMappng != null && !fieldOperation)
            {
                throw new PluginRegistrationIncorrectException($"Field mapping should not be defined for {nameof(IFieldOperation)}");
            }

            _operations.Add(new PluginOperationMetadata(name, typeof(T), description, allowPlayerAccess, () => new T(), fieldMappng));
        }

        public IEnumerable<PluginOperationMetadata> GetOperations() => _operations;

        public abstract string GetName();
        public abstract string GetDescripton();
    }
}
