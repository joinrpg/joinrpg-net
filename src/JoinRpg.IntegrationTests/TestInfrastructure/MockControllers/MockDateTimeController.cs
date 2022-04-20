using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.IntegrationTest.TestInfrastructure.MockControllers
{
    [AllowAnonymous]
    public class MockDateTimeController : Controller
    {
        public static DateTime LastCalledDateTime;

        [HttpPost("test/mockdatetime")]
        public void Test(TestDateTimeModel model)
        {
            LastCalledDateTime = model.Date;
        }
    }

    public class TestDateTimeModel
    {
        public DateTime Date { get; set; }
    }
}
