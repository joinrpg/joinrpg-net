using System;
using JoinRpg.DataModel;

namespace JoinRpg.Services.Interfaces
{
    public interface IUriService
    {
        string Get(ILinkable link);
        Uri GetUri(ILinkable link);
    }
}
