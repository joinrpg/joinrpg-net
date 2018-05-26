using System.Web.Mvc;
using System.Web.Routing;

namespace JoinRpg.Web
{
  public static class RouteConfig
  {
    public static void RegisterRoutes(RouteCollection routes)
    {
      routes.LowercaseUrls = true;
      routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

      routes.MapRoute(name: "Report2D", url: "{ProjectId}/reports/2d/{gameReport2DTemplateId}",
        defaults: new { controller = "Reports", action = "Report2D" });

      routes.MapRoute(name: "PluginSetup", url: "{ProjectId}/plugins",
        defaults: new { controller = "PluginSetup", action = "Index" });

      routes.MapRoute("ForumList", url: "{ProjectId}/forums/",
        defaults: new {controller = "Forum", action = "ListThreads"});

      routes.MapRoute("ForumActions", url: "{ProjectId}/forums/{forumThreadId}/{action}",
        defaults: new {controller = "Forum", action = "ViewThread"});

      routes.MapRoute("ForumByGroup", url: "{ProjectId}/roles/{CharacterGroupId}/forums",
        defaults: new {controller = "Forum", action = "ListThreadsByGroup" });

      routes.MapRoute("ForumByGroupCreate", url: "{ProjectId}/roles/{CharacterGroupId}/create-thread",
        defaults: new {controller = "Forum", action = "createthread"});

      routes.MapRoute("RedirectToDiscussion", url: "{ProjectId}/goto/discussion/{CommentDiscussionId}",
        defaults: new {controller = "Forum", action = "RedirectToDiscussion"});

      routes.MapRoute("RedirectToComment", url: "{ProjectId}/goto/comment/{CommentId}",
        defaults: new { controller = "Forum", action = "RedirectToDiscussion" });

      routes.MapRoute("Print_Char", url: "{ProjectId}/character/{CharacterId}/print",
        defaults: new {controller = "Print", action = "Character"});

      routes.MapRoute(name: "ProjectHome", url: "{ProjectId}/home",
        defaults: new {controller = "Game", action = "Details"});

      routes.MapRoute(name: "ProjectEdit", url: "{ProjectId}/edit",
        defaults: new {controller = "Game", action = "Edit"});

      routes.MapRoute(name: "ProjectClose", url: "{ProjectId}/close",
        defaults: new { controller = "Game", action = "Close" });

      routes.MapRoute(name: "ProjectTools", url: "{ProjectId}/tools/{action}",
        defaults: new { controller = "GameTools", action = "Index" });

      routes.MapRoute(name: "MyClaims", url: "my/claims", defaults: new {controller = "ClaimList", action = "my"});

      routes.MapRoute(name: "GroupAddClaim", url: "{ProjectId}/roles/{CharacterGroupId}/apply",
        defaults: new {controller = "Claim", action = "AddForGroup"});

        routes.MapRoute(name: "GroupAddClaimInRoot", url: "{ProjectId}/apply",
            defaults: new { controller = "Claim", action = "AddForGroup" });

            routes.MapRoute(name: "GroupListClaim", url: "{ProjectId}/roles/{CharacterGroupId}/claims",
        defaults: new {controller = "ClaimList", action = "ListForGroup"});

      routes.MapRoute(name: "GroupListClaimDirect", url: "{ProjectId}/roles/{CharacterGroupId}/discussing",
        defaults: new {controller = "ClaimList", action = "ListForGroupDirect"});

      routes.MapRoute(name: "GroupPlots", url: "{ProjectId}/roles/{CharacterGroupId}/plots",
        defaults: new { controller = "Plot", action = "ForGroup" });

      routes.MapRoute(name: "AddCharacter", url: "{ProjectId}/roles/{CharacterGroupId}/add-char",
        defaults: new {controller = "Character", action = "Create",});

      routes.MapRoute(name: "json_full", url: "{ProjectId}/roles/json_full",
        defaults: new {controller = "GameGroups", action = "AllGroupsJson", includeSpecial = true});

      routes.MapRoute(name: "json_real", url: "{ProjectId}/roles/json_real",
        defaults: new { controller = "GameGroups", action = "AllGroupsJson", includeSpecial = false });

      routes.MapRoute(name: "ProjectGroups", url: "{ProjectId}/groups/{CharacterGroupId}",
        defaults: new { controller = "GameGroups", action = "Locations", CharacterGroupId = UrlParameter.Optional });

      routes.MapRoute(name: "ProjectRoles", url: "{ProjectId}/roles/{CharacterGroupId}",
        defaults: new { controller = "GameGroups", action = "Index", CharacterGroupId = UrlParameter.Optional });

      routes.MapRoute(name: "ProjectRolesAction", url: "{ProjectId}/roles/{CharacterGroupId}/{action}",
        defaults: new {controller = "GameGroups", action = "Index",});

      routes.MapRoute(name: "ProjectAclAdd", url: "acl/add",
        defaults: new { controller = "Acl", action = "Add"});

      routes.MapRoute(name: "ProjectAcls", url: "{ProjectId}/acl/{ProjectAclId}/{action}",
        defaults: new {controller = "Acl", action = "Index", ProjectAclId = UrlParameter.Optional});

      routes.MapRoute(name: "ProjectFieldsCreate", url: "{ProjectId}/fields/create",
        defaults: new
        {
          controller = "GameField",
          action = "Create",
        });

      routes.MapRoute(name: "ProjectFields", url: "{ProjectId}/fields/{ProjectFieldId}/{action}",
        defaults: new {controller = "GameField", action = "Index", ProjectFieldId = UrlParameter.Optional});

      routes.MapRoute(name: "CharacterAddClaim", url: "{ProjectId}/character/{CharacterId}/apply",
        defaults: new {controller = "Claim", action = "AddForCharacter"});

      routes.MapRoute(name: "ClaimResp", url: "{ProjectId}/claims/for-master/{ResponsibleMasterId}",
        defaults: new {controller = "ClaimList", action = "Responsible"});

        routes.MapRoute(name: "ClaimListForRoomType", url: "{ProjectId}/claims/by-roomtype/{roomTypeId}",
            defaults: new { controller = "ClaimList", action = "ListForRoomType" });

        routes.MapRoute(name: "ClaimListForRoomTypeNull", url: "{ProjectId}/claims/without-roomtype/",
            defaults: new { controller = "ClaimList", action = "ListForRoomType" });

            routes.MapRoute(name: "ClaimRespMaster", url: "{ProjectId}/claims/problems-for-master/{ResponsibleMasterId}",
        defaults: new { controller = "ClaimList", action = "ResponsibleProblems" });


      routes.MapRoute(name: "ClaimCheckIn", url: "{ProjectId}/claim/{ClaimId}/checkin",
        defaults: new { controller = "CheckIn", action = "CheckIn" });

      routes.MapRoute(name: "SecondRole", url: "{ProjectId}/claim/{ClaimId}/secondrole",
        defaults: new { controller = "CheckIn", action = "SecondRole" });

      routes.MapRoute(name: "Claim", url: "{ProjectId}/claim/{ClaimId}/{action}",
        defaults: new { controller = "Claim", action = "Edit" });

      routes.MapRoute(name: "CheckIn", url: "{ProjectId}/checkin/{action}",
        defaults: new { controller = "Checkin", action = "Index" });


      routes.MapRoute(name: "MyClaim", url: "{ProjectId}/myclaim",
        defaults: new { controller = "Claim", action = "MyClaim" });

      routes.MapRoute(name: "CharacterActionsByGroup", url: "{ProjectId}/characters/bygroup/{CharacterGroupId}",
        defaults: new { controller = "CharacterList", action = "ByGroup" });

      routes.MapRoute(name: "CharacterActions", url: "{ProjectId}/characters/{action}",
        defaults: new {controller = "CharacterList", action = "Index"});   

      routes.MapRoute(name: "ClaimActions", url: "{ProjectId}/claims/{action}",
        defaults: new { controller = "ClaimList", action = "Index" });

      routes.MapRoute(name: "PlotWithId", url: "{ProjectId}/plot/{PlotFolderId}/{action}",
        defaults: new {controller = "Plot", action = "Index"});

      routes.MapRoute(name: "PlotShowVersion", url: "{ProjectId}/plot/{PlotFolderId}/{PlotElementId}/version/{version}",
        defaults: new { controller = "Plot", action = "ShowElementVersion" });

      routes.MapRoute(name: "PlotByTag", url: "{ProjectId}/plots/tag/{tagName}",
       defaults: new { controller = "Plot", action = "ByTag" });

      routes.MapRoute(name: "Plot", url: "{ProjectId}/plots/{action}",
        defaults: new {controller = "Plot", action = "Index"});

      routes.MapRoute(name: "Character", url: "{ProjectId}/character/{CharacterId}/{action}",
        defaults: new {controller = "Character", action = "Details"});

      routes.MapRoute(name: "FinanceExport", url: "{ProjectId}/money.csv/{action}/{token}",
        defaults: new { controller = "Finances", action = "MoneySummary" });

      routes.MapRoute(name: "Finances", url: "{ProjectId}/money/{action}",
        defaults: new { controller = "Finances", action = "Setup" });

      routes.MapRoute(name: "PrintCOntroller", url: "{ProjectId}/print/{action}",
        defaults: new { controller = "Print", action = "Index" });

      routes.MapRoute(name: "UserMe", url: "user/me",
        defaults: new { controller = "User", action = "Me"});

      routes.MapRoute(name: "User", url: "user/{UserId}/{action}",
        defaults: new {controller = "User", action = "Details"});

        routes.MapRoute(name: "AccommodationTypeDetails", url: "{ProjectId}/rooms/{roomTypeId}/details",
            defaults: new { controller = "accommodationtype", action = "EditRoomTypeRooms" });

        routes.MapRoute(name: "AccommodationTypeEdit", url: "{ProjectId}/rooms/{roomTypeId}/edit",
            defaults: new { controller = "accommodationtype", action = "EditRoomType" });

            routes.MapRoute(name: "AccommodationType", url: "{ProjectId}/rooms/{action}",
            defaults: new { controller = "accommodationtype", action = "Index" });

        routes.MapRoute(name: "ClaimAccommodation", url: "{ProjectId}/accommodation/{ClaimId}",
            defaults: new { controller = "ClaimAccommodation", action = "Index" });

            routes.MapRoute(
        name: "Default",
        url: "{controller}/{action}/{id}",
        defaults: new {controller = "Home", action = "Index", id = UrlParameter.Optional, area = ""}
        );

    }
  }
}
