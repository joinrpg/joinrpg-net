namespace JoinRpg.Common.WebComponents.Test;

/// <summary>
/// Находит корень репозитория — тестам иконок нужно читать файлы исходников.
/// </summary>
internal static class RepositoryLocator
{
    internal static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Joinrpg.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Не найден корень репозитория (Joinrpg.slnx)");
    }
}
