namespace JoinRpg.WebComponents.Test;

public class JoinAdminAuthorizeViewTest
{
    [Fact]
    public void AdminUser_ShouldSeeChildContent()
    {
        using var ctx = new BunitContext();
        var authContext = ctx.AddAuthorization();
        authContext.SetAuthorized("admin@test.com");
        authContext.SetRoles("admin");

        var cut = ctx.Render<JoinAdminAuthorizeView>(parameters =>
                {
                    parameters.Add(x => x.Roles, "admin");
                    parameters.AddChildContent("<p>Секретный контент</p>");
                }
            );

        cut.Markup.ShouldContain("Секретный контент");
        cut.Markup.ShouldNotContain("Доступ запрещён");
    }

    [Fact]
    public void NonAdminUser_ShouldSeeDeniedMessage()
    {
        using var ctx = new BunitContext();
        var authContext = ctx.AddAuthorization();
        authContext.SetAuthorized("user@test.com");

        var cut = ctx.Render<JoinAdminAuthorizeView>(parameters =>
            {
                parameters.Add(x => x.Roles, "admin");
                parameters.AddChildContent("<p>Секретный контент</p>");
            }
            );

        cut.Markup.ShouldContain("Доступ запрещён");
        cut.Markup.ShouldNotContain("Секретный контент");
    }

    [Fact]
    public void AnonymousUser_ShouldSeeDeniedMessage()
    {
        using var ctx = new BunitContext();
        ctx.AddAuthorization();

        var cut = ctx.Render<JoinAdminAuthorizeView>(parameters =>
            {
                parameters.Add(x => x.Roles, "admin");
                parameters.AddChildContent("<p>Секретный контент</p>");
            }
            );

        cut.Markup.ShouldContain("Доступ запрещён");
        cut.Markup.ShouldNotContain("Секретный контент");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyRoles_ShouldThrow(string roles)
    {
        using var ctx = new BunitContext();
        ctx.AddAuthorization();

        Should.Throw<ArgumentException>(() =>
            ctx.Render<JoinAdminAuthorizeView>(parameters =>
            {
                parameters.Add(x => x.Roles, roles);
                parameters.AddChildContent("<p>Контент</p>");
            })
        );
    }
}
