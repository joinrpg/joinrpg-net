using Microsoft.EntityFrameworkCore;

namespace JoinRpg.IdPortal.OAuthServer;

public class IdPortalDbContext(DbContextOptions<IdPortalDbContext> options) : DbContext(options)
{
}
