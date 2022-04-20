// ReSharper disable once CheckNamespace
namespace PscbApi
{
    /// <summary>
    /// Extensions for enums
    /// </summary>
    public static class EnumExtensions
    {
        /// <summary>
        /// Returns list of attributes of type <typeparamref name="T"/> associated with enum value
        /// </summary>
        public static IEnumerable<T> GetCustomAttributes<T>(this Enum self)
            where T : Attribute
            => self.GetType()
                .GetField(self.ToString())
                .GetCustomAttributes(typeof(T), true)
                .Cast<T>();

        /// <summary>
        /// Returns first attribute of specified type <typeparamref name="T"/>
        /// </summary>
        public static T GetCustomAttribute<T>(this Enum self)
            where T : Attribute
            => self.GetCustomAttributes<T>()
                .FirstOrDefault();

        /// <summary>
        /// Returns identifier associated using <see cref="IdentifierAttribute"/> to enumeration value
        /// </summary>
        public static string GetIdentifier(this Enum self)
            => self.GetCustomAttribute<IdentifierAttribute>()?.Identifier ?? Enum.GetName(self.GetType(), self);
    }
}
