using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Joinrpg.Web.Identity;
using JoinRpg.Portal.Identity;
using JoinRpg.Portal.Infrastructure.Authentication;
using JoinRpg.Web.XGameApi.Contract;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace JoinRpg.Portal.Controllers.XGameApi;

[ApiController]
public class XApiLoginController : ControllerBase
{
    private readonly ApplicationUserManager userManager;
    private readonly JwtSecretOptions secret;
    private readonly JwtBearerOptions jwt;

    public XApiLoginController(ApplicationUserManager userManager, IOptions<JwtBearerOptions> jwt, IOptions<JwtSecretOptions> secret)
    {
        this.userManager = userManager;
        this.secret = secret.Value;
        this.jwt = jwt.Value;
    }

    [HttpPost("/x-api/token")]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(400)]
    [ProducesResponseType(200)]
    public async Task<ActionResult> Login(
        [FromForm] string username,
        [FromForm] string password,
        [FromForm(Name = "grant_type")] string grantType)
    {
        if (grantType != "password")
        {
            return BadRequest();
        }

        var user = await userManager.FindByEmailAsync(username);
        if (user == null)
        {
            return Forbid();
        }
        if (!await userManager.CheckPasswordAsync(user, password))
        {
            return Forbid();
        }
        JwtSecurityToken jwtSecurityToken = await CreateJwtToken(user);
        return Ok(new AuthenticationResponse
        {
            access_token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken),
            expires_in = (int)Math.Floor(secret.JwtLifetime.TotalSeconds),
        });
    }

    private async Task<JwtSecurityToken> CreateJwtToken(JoinIdentityUser user)
    {
        var userClaims = await userManager.GetClaimsAsync(user);
        var roles = await userManager.GetRolesAsync(user);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.UserName),
            new Claim("uid", user.Id.ToString()),
        }
        .Union(userClaims)
        .Union(roles.Select(r => new Claim("roles", r)));


        var symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret.SecretKey));
        var signingCredentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);
        var jwtSecurityToken = new JwtSecurityToken(
            issuer: secret.Issuer,
            audience: jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(secret.JwtLifetime),
            signingCredentials: signingCredentials);
        return jwtSecurityToken;
    }
}
