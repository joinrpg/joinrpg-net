using System.Collections;

namespace JoinRpg.TestHelpers
{
    /// <summary>
    /// Theory to call unit test with all possible enum values
    /// </summary>
    /// <typeparam name="TEnum"></typeparam>
    public class EnumTheoryDataGenerator<TEnum> : IEnumerable<object[]> where TEnum : struct, Enum
    {
        private readonly List<object[]> data = Enum.GetValues<TEnum>().Select(x => new object[] { x }).ToList();

        /// <inheritdoc />
        public IEnumerator<object[]> GetEnumerator() => data.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => data.GetEnumerator();
    }
}
