using JoinRpg.Common.EntityFrameworkCore;
using JoinRpg.Common.PrimitiveTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace JoinRpg.Common.EntityFrameworkCore.Test;

public class EntityIdConversionExtensionsTest
{
    private class Widget
    {
        public UserIdentification Id { get; set; } = null!;
        public TelegramChatId ChatId { get; set; } = null!;
    }

    private class WidgetContext(DbContextOptions<WidgetContext> options) : DbContext(options)
    {
        public DbSet<Widget> Widgets => Set<Widget>();

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Properties<UserIdentification>().HaveEntityIdValueConversion<UserIdentification, int>();
            configurationBuilder.Properties<TelegramChatId>().HaveEntityIdValueConversion<TelegramChatId, long>();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.EntityIdsSetValueGeneratedOnAdd();
        }
    }

    private static WidgetContext CreateContext() =>
        new(new DbContextOptionsBuilder<WidgetContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public void ShouldRoundTripIntAndLongBasedIds()
    {
        using var context = CreateContext();
        context.Widgets.Add(new Widget { Id = new UserIdentification(42), ChatId = new TelegramChatId(-1004315256401) });
        context.SaveChanges();

        var widget = context.Widgets.Single();

        widget.Id.ShouldBe(new UserIdentification(42));
        widget.ChatId.ShouldBe(new TelegramChatId(-1004315256401));
    }

    [Fact]
    public void ShouldMarkSingleColumnPrimaryKeyAsValueGeneratedOnAdd()
    {
        using var context = CreateContext();

        var keyProperty = context.Model.FindEntityType(typeof(Widget))!.FindPrimaryKey()!.Properties.Single();

        keyProperty.ValueGenerated.ShouldBe(ValueGenerated.OnAdd);
    }
}
