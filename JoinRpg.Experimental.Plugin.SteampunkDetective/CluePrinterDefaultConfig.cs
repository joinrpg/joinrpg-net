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
            AllowedFieldValues = new [] {Deutsch, Vengr, Poland}
          },
          new SignDefinition()
          {
            SignType = SignType.FieldBased,
            Code = 207,
            FieldId = Nation,
            AllowedFieldValues = new [] {Poland, Serbia}
          },
          new SignDefinition()
          {
            SignType = SignType.FieldBased,
            Code = 206,
            FieldId = Nation,
            AllowedFieldValues = new [] {Vengr, Czech, Romania}
          },

          new SignDefinition()
          {
            SignType = SignType.FieldBased,
            Code = 208,
            FieldId = Nation,
            AllowedFieldValues = new [] {Serbia, Czech, Russia}
          },new SignDefinition()
          {
            SignType = SignType.FieldBased,
            Code = 209,
            FieldId = Nation,
            AllowedFieldValues = new [] {Gipsie, Romania, NationOther, Jews}
          },
          new SignDefinition()
          {
            SignType = SignType.FieldBased,
            Code = 301,
            FieldId = 282,
            AllowedFieldValues = new [] {"487", "488",}
          },
          new SignDefinition()
          {
            SignType = SignType.FieldBased,
            Code = 302,
            FieldId = 282,
            AllowedFieldValues = new [] {"488", "489"}
          },
          new SignDefinition()
          {
            SignType = SignType.FieldBased,
            Code = 303,
            FieldId = 282,
            AllowedFieldValues = new [] {"487", "489"}
          },
        },
        RequiredClues = 5,
        MaxMeaningfulClues = 3
      };
    }
  }
}