using JoinRpg.DomainTypes;
using JoinRpg.Web.Models.Print;
using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.WebPortal.Models.Test.Print;

public class PrintChunksTest
{
    private static readonly ProjectIdentification ProjectId = new(1);

    private static CharacterIdentification[] Characters(int count)
        => [.. Enumerable.Range(1, count).Select(id => new CharacterIdentification(ProjectId, id))];

    [Fact]
    public void Small_OnlyAll()
        => PrintChunks.Split(Characters(5), chunkSize: 10).Select(c => c.Name).ShouldBe(["[все]"]);

    // Ровно в размер пачки — пачки не нужны, хватает «[все]».
    [Fact]
    public void ExactlyChunkSize_OnlyAll()
        => PrintChunks.Split(Characters(10), chunkSize: 10).Select(c => c.Name).ShouldBe(["[все]"]);

    [Fact]
    public void Large_AllAndChunks()
    {
        var chunks = PrintChunks.Split(Characters(25), chunkSize: 10).ToList();

        chunks.Select(c => c.Name).ShouldBe(["[все]", "[1–10]", "[11–20]", "[21–25]"]);
        CompressedIntList.Parse(chunks[3].CharacterIds, null).List.ShouldBe([21, 22, 23, 24, 25]);
    }
}
