namespace JoinRpg.TestHelpers;

/// <summary>
/// SQL Server, на котором работает прод: тесты на Testcontainers поднимают ту же сборку.
/// <para>
/// На проде — SQL Server 2019 CU32-GDR (15.0.4490.9), Express на Ubuntu 20.04, поставлен вручную
/// на ВМ <c>mssql-prod</c> в Selectel. Тег <c>GDR11</c> выбран по метке <c>com.microsoft.version</c>
/// образа: она совпадает с <c>SERVERPROPERTY('ProductVersion')</c> прода.
/// </para>
/// <para>
/// Выровнены только версия и сборка. Редакция в тестах — Developer (дефолт образа): лимиты Express
/// тестам только мешают, но возможности старших редакций (например, <c>ONLINE = ON</c>) пройдут
/// в тестах и упадут на проде.
/// </para>
/// <para>
/// <b>Менять только вместе с продом</b> и вместе с образом <c>sql-server</c> в <c>docker-compose.yml</c>.
/// Если тег разъедется с продом, тесты будут проверять среду, которой нигде нет.
/// </para>
/// </summary>
public static class ProductionSqlServer
{
    /// <summary>
    /// Образ для <c>MsSqlBuilder</c>.
    /// </summary>
    public const string Image = "mcr.microsoft.com/mssql/server:2019-CU32-GDR11-ubuntu-20.04";

    /// <summary>
    /// Collation базы <c>joinrpg-prod</c>. У самого сервера прода collation другой —
    /// <c>SQL_Latin1_General_CP1_CI_AS</c>, он же дефолт образа, поэтому в тестах сервер не настраивается,
    /// а базу создают с этим collation явно.
    /// <para>
    /// Пока так делает только IntegrationTest. IdPortal.Test накатывает миграции в <c>master</c>
    /// контейнера, и там остаются дефолты сервера.
    /// </para>
    /// </summary>
    public const string DatabaseCollation = "Cyrillic_General_CI_AS";
}
