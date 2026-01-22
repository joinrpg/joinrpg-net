namespace JoinRpg.Integrations.KogdaIgra.Test;

public class GameDataParserTest
{
    [Fact]
    public void TestParseWithoutUpdateDate()
    {
        var str = """                                    
            {"id":440,"name":"Battlestar Galactica: \u0412 \u043f\u043e\u0438\u0441\u043a\u0430\u0445 \u0434\u043e\u043c\u0430","uri":"http:\/\/bsg.bastilia.ru\/final\/","type":3,"polygon":"140","mg":"\u041c\u0422\u0413 \u00ab\u0411\u0430\u0441\u0442\u0438\u043b\u0438\u044f\u00bb","email":"bsg@bastilia.ru","status":1,"comment":"","sub_region_id":"2","deleted_flag":"0","players_count":99,"allrpg_info_id":1356,"vk_club":null,"lj_comm":null,"fb_comm":null,"polygon_name":"\u0431\/\u043e\u00a0\u00ab\u041c\u0435\u0447\u0442\u0430\u00bb, \u041c\u0438\u0447\u0443\u0440\u0438\u043d\u0441\u043a\u043e\u0435","game_type_name":"\u041d\u0430&nbsp;\u0442\u0443\u0440\u0431\u0430\u0437\u0435","sub_region_disp_name":"\u0421\u041f\u0431","sub_region_name":"\u041b\u0435\u043d\u0438\u043d\u0433\u0440\u0430\u0434\u0441\u043a\u0430\u044f \u043e\u0431\u043b\u0430\u0441\u0442\u044c","status_name":"\u041f\u0440\u043e\u0448\u043b\u0430","begin":"2010-09-03","time":"3"}
            """;

        var gameInfo = ResultParser.ParseGameInfo(str);
        gameInfo.ShouldNotBeNull();
        gameInfo.Id.ShouldBe(440);
        gameInfo.GameData.ShouldBe(str);
        gameInfo.Name.ShouldBe("Battlestar Galactica: В поисках дома");
    }

    [Fact]
    public void TestParse()
    {
        var str = """                        
            
            {"id":440,"name":"Battlestar Galactica: \u0412 \u043f\u043e\u0438\u0441\u043a\u0430\u0445 \u0434\u043e\u043c\u0430","uri":"http:\/\/bsg.bastilia.ru\/final\/","type":3,"polygon":"140","mg":"\u041c\u0422\u0413 \u00ab\u0411\u0430\u0441\u0442\u0438\u043b\u0438\u044f\u00bb","email":"bsg@bastilia.ru","status":1,"comment":"","sub_region_id":"2","deleted_flag":"0","players_count":99,"allrpg_info_id":1356,"vk_club":null,"lj_comm":null,"fb_comm":null,"polygon_name":"\u0431\/\u043e\u00a0\u00ab\u041c\u0435\u0447\u0442\u0430\u00bb, \u041c\u0438\u0447\u0443\u0440\u0438\u043d\u0441\u043a\u043e\u0435","game_type_name":"\u041d\u0430&nbsp;\u0442\u0443\u0440\u0431\u0430\u0437\u0435","sub_region_disp_name":"\u0421\u041f\u0431","sub_region_name":"\u041b\u0435\u043d\u0438\u043d\u0433\u0440\u0430\u0434\u0441\u043a\u0430\u044f \u043e\u0431\u043b\u0430\u0441\u0442\u044c","status_name":"\u041f\u0440\u043e\u0448\u043b\u0430","begin":"2010-09-03","time":"3","update_date":"2013-03-17T23:56:21+00:00"}
            """;

        var gameInfo = ResultParser.ParseGameInfo(str);
        gameInfo.ShouldNotBeNull();
        gameInfo.Id.ShouldBe(440);
        gameInfo.GameData.ShouldBe(str);
        gameInfo.Name.ShouldBe("Battlestar Galactica: В поисках дома");
        gameInfo.MasterGroupName.ShouldBe("МТГ «Бастилия»");
        gameInfo.RegionName.ShouldBe("СПб");
        gameInfo.Begin.ShouldBe(new DateOnly(2010, 09, 03));
        gameInfo.End.ShouldBe(new DateOnly(2010, 09, 05));
        gameInfo.UpdateDate.ShouldBe(new DateTimeOffset(2013, 03, 17, 23, 56, 21, TimeSpan.FromHours(0)));
    }
}
