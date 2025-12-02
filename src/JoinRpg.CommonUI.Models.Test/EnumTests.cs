using JoinRpg.TestHelpers;
using JoinRpg.Web.Models;
using Xunit;

namespace JoinRpg.CommonUI.Models.Test;

public class EnumTests
{
    [Fact]
    public void CommentExtraActionEnum()
    {
        EnumerationTestComparer.EnsureSame<PrimitiveTypes.Claims.CommentExtraAction, CommentExtraAction>();
    }

    [Fact]
    public void PaymentEnum()
    {
        EnumerationTestComparer.EnsureSame<DataModel.PaymentTypeKind, PaymentTypeKindViewModel>();
    }
}
