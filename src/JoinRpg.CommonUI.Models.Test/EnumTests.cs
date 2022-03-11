using JoinRpg.TestHelpers;
using JoinRpg.Web.Models;
using Xunit;

namespace JoinRpg.CommonUI.Models.Test
{
    public class EnumTests
    {
        [Fact]
        public void ProblemEnum()
        {
            EnumerationTestComparer.EnsureSame<DataModel.CommentExtraAction, CommentExtraAction>();
            EnumerationTestComparer.EnsureSame<DataModel.PaymentTypeKind, PaymentTypeKindViewModel>();
        }
    }
}
