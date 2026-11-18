using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.Web.Models.Print;

/// <summary>Пачка персонажей для одной ссылки на печать</summary>
/// <param name="Name">Подпись ссылки: «[все]» или «[1–100]»</param>
/// <param name="CharacterIds">Персонажи пачки в сжатом виде — значение параметра characterIds</param>
public record PrintChunk(string Name, string CharacterIds);

/// <summary>
/// Печать большого проекта одной страницей тяжела, поэтому рядом со ссылкой «на всех» даём ссылки по пачкам.
/// </summary>
public static class PrintChunks
{
    public static IEnumerable<PrintChunk> Split(IReadOnlyCollection<CharacterIdentification> characterIds, int chunkSize = 100)
    {
        yield return new PrintChunk("[все]", new CompressedIntList(characterIds).ToString());

        if (characterIds.Count <= chunkSize)
        {
            yield break;
        }

        var index = 0;
        foreach (var chunk in characterIds.Chunk(chunkSize))
        {
            yield return new PrintChunk($"[{index + 1}–{index + chunk.Length}]", new CompressedIntList(chunk).ToString());
            index += chunk.Length;
        }
    }
}
