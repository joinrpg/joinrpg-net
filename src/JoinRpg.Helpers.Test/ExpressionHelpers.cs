using System.Linq.Expressions;
using Shouldly;
using Xunit;

namespace JoinRpg.Helpers.Test
{

    public class ExpressionHelpers
    {
        // ReSharper disable once ClassNeverInstantiated.Local
        private class AnonClass
        {
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public int Prop { get; set; }
        }

        [Fact]
        public void TestAsPropertyName()
        {
            Expression<Func<AnonClass, int>> lambda = foo => foo.Prop;

            lambda.AsPropertyName().ShouldBe("Prop");
        }
    }
}
