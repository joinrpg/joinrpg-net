namespace JoinRpg.Web.Models;

public class FinanceOperationAdminFunctionsViewModel
{
    public bool Allowed { get; }

    public bool PreferentialFeeEnabled { get; }

    public bool PreferentialFeeUser { get; }

    public FinanceOperationAdminFunctionsViewModel(ClaimFeeViewModel source)
    {
        Allowed = source.HasFeeAdminAccess;
        PreferentialFeeEnabled = source.PreferentialFeeEnabled;
        PreferentialFeeUser = source.PreferentialFeeUser;
    }
}
