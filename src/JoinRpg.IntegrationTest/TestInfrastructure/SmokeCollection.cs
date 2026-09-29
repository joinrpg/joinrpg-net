namespace JoinRpg.IntegrationTest.TestInfrastructure;

/// <summary>
/// Коллекция сценариев, работающих на общем сид-проекте <see cref="SmokeProjectFixture"/>.
/// </summary>
/// <remarks>
/// Сид дорогой: поднять хост, накатить миграции и наполнить проект — самая долгая часть смоука,
/// а страницы только читают. Пока фикстура была <c>IClassFixture</c>, второй сценарий на том же
/// сиде означал второй прогон наполнения; коллекция даёт один сид на все такие сценарии.
/// </remarks>
[CollectionDefinition(Name)]
public class SmokeCollection : ICollectionFixture<SmokeProjectFixture>
{
    public const string Name = "Smoke";
}
