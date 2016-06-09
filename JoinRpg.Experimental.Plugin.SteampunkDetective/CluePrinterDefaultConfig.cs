using System.Collections.Generic;

namespace JoinRpg.Experimental.Plugin.SteampunkDetective
{
  internal static class CluePrinterDefaultConfig
  {
    private const int Nation = 30;
    private const int Sex = 285;
    private const string Ireland = "49";
    private const string Scotland = "50";
    private const string Jews = "246";
    private const string English = "48";
    private const string Deutsch = "51";
    private const string Vengr = "57";
    private const string Poland = "75";
    private const string Czech = "58";
    private const string Romania = "59";
    private const string Serbia = "60";
    private const string Russia = "61";
    private const string NationOther = "62";
    private const string Gipsie = "456";

    public static ClueConfiguration GetDefaultConfig()
    {
      return new ClueConfiguration()
      {
        CluePerCharacter = 5,
        Digits = 3,
        SignDefinitions = new List<SignDefinition>()
        {
          new SignDefinition
          {
            SignType = SignType.FieldBased,
            Code = 101,
            FieldId = Sex,
            AllowedFieldValues = new[] {"498"}
          },
          new SignDefinition
          {
            SignType = SignType.FieldBased,
            Code = 102,
            FieldId = Sex,
            AllowedFieldValues = new[] {"499"}
          },
          new SignDefinition
          {
            SignType = SignType.FieldBased,
            Code = 201,
            FieldId = Nation,
            AllowedFieldValues = new[] {English, Deutsch, Russia, NationOther}
          },
          new SignDefinition
          {
            SignType = SignType.FieldBased,
            Code = 202,
            FieldId = Nation,
            AllowedFieldValues = new[] {English, Ireland, Scotland}
          },
          new SignDefinition
          {
            SignType = SignType.FieldBased,
            Code = 203,
            FieldId = Nation,
            AllowedFieldValues = new[] {Ireland, Gipsie}
          },
          new SignDefinition
          {
            SignType = SignType.FieldBased,
            Code = 204,
            FieldId = Nation,
            AllowedFieldValues = new[] {Scotland, Jews}
          },
          new SignDefinition
          {
            SignType = SignType.FieldBased,
            Code = 205,
            FieldId = Nation,
            AllowedFieldValues = new[] {Deutsch, Vengr, Poland}
          },
          new SignDefinition()
          {
            SignType = SignType.FieldBased,
            Code = 207,
            FieldId = Nation,
            AllowedFieldValues = new[] {Poland, Serbia}
          },
          new SignDefinition()
          {
            SignType = SignType.FieldBased,
            Code = 206,
            FieldId = Nation,
            AllowedFieldValues = new[] {Vengr, Czech, Romania}
          },

          new SignDefinition()
          {
            SignType = SignType.FieldBased,
            Code = 208,
            FieldId = Nation,
            AllowedFieldValues = new[] {Serbia, Czech, Russia}
          },
          new SignDefinition()
          {
            SignType = SignType.FieldBased,
            Code = 209,
            FieldId = Nation,
            AllowedFieldValues = new[] {Gipsie, Romania, NationOther, Jews}
          },
          new SignDefinition()
          {
            SignType = SignType.FieldBased,
            Code = 301,
            FieldId = 282,
            AllowedFieldValues = new[] {"487", "488",}
          },
          new SignDefinition()
          {
            SignType = SignType.FieldBased,
            Code = 302,
            FieldId = 282,
            AllowedFieldValues = new[] {"488", "489"}
          },
          new SignDefinition()
          {
            SignType = SignType.FieldBased,
            Code = 303,
            FieldId = 282,
            AllowedFieldValues = new[] {"487", "489"}
          },
          new SignDefinition()
          {
            SignType = SignType.FieldBased,
            Code = 401,
            FieldId = 287,
            AllowedFieldValues = new[] {"501", "502"}
          },
          new SignDefinition()
          {
            SignType = SignType.FieldBased,
            Code = 402,
            FieldId = 287,
            AllowedFieldValues = new[] {"502", "503"}
          },
          new SignDefinition()
          {
            SignType = SignType.FieldBased,
            Code = 403,
            FieldId = 287,
            AllowedFieldValues = new[] {"501", "503"}
          },

          new SignDefinition()
          {
            SignType = SignType.FieldBased,
            Code = 501,
            FieldId = 293,
            AllowedFieldValues = new[] {"490", "493", "494"}
          },
          new SignDefinition()
          {
            SignType = SignType.FieldBased,
            Code = 502,
            FieldId = 293,
            AllowedFieldValues = new[] {"491"}
          },
          new SignDefinition()
          {
            SignType = SignType.FieldBased,
            Code = 503,
            FieldId = 293,
            AllowedFieldValues = new[] {"492", "493"}
          },
          new SignDefinition()
          {
            SignType = SignType.FieldBased,
            Code = 505,
            FieldId = 293,
            AllowedFieldValues = new[] {"490", "491"}
          },
          new SignDefinition()
          {
            SignType = SignType.FieldBased,
            Code = 506,
            FieldId = 293,
            AllowedFieldValues = new[] {"495", "494"}
          },
          new SignDefinition()
          {
            SignType = SignType.FieldBased,
            Code = 504,
            FieldId = 293,
            AllowedFieldValues = new[] {"492", "495"}
          },

          new SignDefinition()
          {
            SignType = SignType.FieldBased,
            Code = 601,
            FieldId = 290,
            AllowedFieldValues = new[] {"512", "513"}
          },
          new SignDefinition()
          {
            SignType = SignType.FieldBased,
            Code = 602,
            FieldId = 290,
            AllowedFieldValues = new[] {"513", "514"}
          },
          new SignDefinition()
          {
            SignType = SignType.FieldBased,
            Code = 603,
            FieldId = 290,
            AllowedFieldValues = new[] {"512", "514"}
          },

          new SignDefinition()
          {
            SignType = SignType.FieldBased,
            Code = 701,
            FieldId = 284,
            AllowedFieldValues = new[] {"496",}
          },
          new SignDefinition()
          {
            SignType = SignType.FieldBased,
            Code = 702,
            FieldId = 284,
            AllowedFieldValues = new[] {"497",}
          },
          new SignDefinition()
          {
            SignType = SignType.FieldBased,
            Code = 703,
            FieldId = 284,
            AllowedFieldValues = new[] {"515",}
          },

           new SignDefinition()
          {
            SignType = SignType.FieldBased,
            Code = 704,
            FieldId = 288,
            AllowedFieldValues = new[] {"504",}
          },
          new SignDefinition()
          {
            SignType = SignType.FieldBased,
            Code = 705,
            FieldId = 288,
            AllowedFieldValues = new[] {"505",}
          },
          new SignDefinition()
          {
            SignType = SignType.FieldBased,
            Code = 706,
            FieldId = 288,
            AllowedFieldValues = new[] {"506",}
          }, new SignDefinition()
          {
            SignType = SignType.FieldBased,
            Code = 707,
            FieldId = 288,
            AllowedFieldValues = new[] {"507",}
          },
          new SignDefinition()
          {
            SignType = SignType.FieldBased,
            Code = 708,
            FieldId = 288,
            AllowedFieldValues = new[] {"508",}
          },
          new SignDefinition()
          {
            SignType = SignType.FieldBased,
            Code = 709,
            FieldId = 288,
            AllowedFieldValues = new[] {"509",}
          },

          new SignDefinition()
          {
            SignType = SignType.GroupBased,
            Code = 801,
            AllowedFieldValues = new [] {"617"}
          },
          new SignDefinition()
          {
            SignType = SignType.GroupBased,
            Code = 802,
            AllowedFieldValues = new [] {"625"}
          },
          new SignDefinition()
          {
            SignType = SignType.GroupBased,
            Code = 803,
            AllowedFieldValues = new [] {"394"}
          },
          new SignDefinition()
          {
            SignType = SignType.GroupBased,
            Code = 804,
            AllowedFieldValues = new [] {"396"}
          },
          new SignDefinition()
          {
            SignType = SignType.GroupBased,
            Code = 805,
            AllowedFieldValues = new [] {"395"}
          },
        },
        RequiredClues = 5,
        MaxMeaningfulClues = 3
      };
    }
  }
}