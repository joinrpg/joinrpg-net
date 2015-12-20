using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using JoinRpg.Helpers;
using Newtonsoft.Json;

namespace JoinRpg.Services.Impl.Allrpg
{
  internal class AllrpgApi
  {
    public enum Status
    {
      Success,
      NetworkError,
      ParseError,
      NoSuchUser,
      WrongKey
    }

    public sealed class Reply<T>
    {
      public Reply(Status status, string rawResult = null, T result = default(T))
      {
        Status = status;
        RawResult = rawResult;
        Result = result;
      }

      public Status Status { get; }

      public string RawResult {get; }
      public T Result { get;  }
    }

    private readonly string _apiKey;

    public AllrpgApi(string apiKey)
    {
      _apiKey = apiKey;
    }

    private static async Task<Reply<T>> Call<T>(string action, string args)
    {
      string rawResult;

      try
      {
        rawResult =
          await new WebClient().DownloadStringTaskAsync(new Uri($"http://allrpg.info/joinrpg/{action}.php?{args}"));
      }
      catch (Exception)
      {
        return new Reply<T>(Status.NetworkError);
      }

      switch (rawResult)
      {
        case "ERROR_WRONG_KEY":
          return new Reply<T>(Status.WrongKey, rawResult);
        case "ERROR_NO_SUCH_USER":
          return new Reply<T>(Status.NoSuchUser, rawResult);
      }

      try
      {
        var result = JsonConvert.DeserializeObject<T>(rawResult);
        return result == null
          ? new Reply<T>(Status.ParseError, rawResult)
          : new Reply<T>(Status.Success, rawResult, result);
      }
      catch
      {
        return new Reply<T>(Status.ParseError, rawResult);
      }
    }

    private static string SignPayload(string payload, string key) => (payload + key).ToHexHash(SHA1.Create());

    //Yes, yes, MD5 without salt. Send you complaints to Cetb
    private static string AllrpgLegacyHash(string password) => password.ToHexHash(MD5.Create());

    public Task<Reply<ProfileReply>> GetProfile(string email)
    {
      var key = SignPayload(email, _apiKey);

      return Call<ProfileReply>("profile", $"email={email}&key={key}");
    }

    public Task<Reply<PasswordReply>> CheckPassword(string email, string password)
    {
      var key = SignPayload(email + "@" + AllrpgLegacyHash(password), _apiKey);

      return Call<PasswordReply>("checkpassword", $"email={email}&key={key}");
    }

    public Task<Reply<ProjectReply>>  DownloadProject(int allrpgId)
    {
      var key = SignPayload(allrpgId.ToString(), _apiKey);
      return Call<ProjectReply>("importproject", $"id={allrpgId}&key={key}");
    }
  }
}