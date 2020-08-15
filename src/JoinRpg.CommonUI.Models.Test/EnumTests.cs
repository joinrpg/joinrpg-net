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
            EnumerationTestHelper.CheckEnums<DataModel.CommentExtraAction, CommentExtraAction>();
            EnumerationTestHelper.CheckEnums<DataModel.PaymentTypeKind, PaymentTypeKindViewModel>();
        }
    }
}
