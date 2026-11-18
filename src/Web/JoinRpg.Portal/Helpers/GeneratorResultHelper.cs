using JoinRpg.Helpers;
using JoinRpg.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Helpers;

public static class GeneratorResultHelper
{
    public static FileContentResult Result(string fileName, IExportGenerator generator)
        => new(generator.Generate(), generator.ContentType) { FileDownloadName = Path.ChangeExtension(fileName.ToSafeFileName(), generator.FileExtension) };
}
