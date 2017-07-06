using System;

namespace JoinRpg.Experimental.Plugin.Interfaces
{
  public class PluginConfigurationIncorrectException : ApplicationException
  {
    public PluginConfigurationIncorrectException(string message = "Unknown configuration error") : base(message)
    {
    }

  }

  public class PluginRegistrationIncorrectException : ApplicationException
  {
    public PluginRegistrationIncorrectException(string message) : base(message)
    {
    }
  }
}
