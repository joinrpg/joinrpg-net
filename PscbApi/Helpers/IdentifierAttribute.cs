using System;

// ReSharper disable once CheckNamespace
namespace PscbApi
{
    /// <summary>
    /// Allows to assign enum value with identifier
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class IdentifierAttribute : Attribute
    {
        /// <summary>
        /// Identifier
        /// </summary>
        public string Identifier { get; }

        /// <summary>
        /// Creates new Identifier attribute
        /// </summary>
        public IdentifierAttribute(string identifier) => Identifier = identifier;
    }
}
