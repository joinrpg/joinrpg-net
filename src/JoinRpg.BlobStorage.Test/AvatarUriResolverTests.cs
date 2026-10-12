using JoinRpg.Services.Interfaces;
using Shouldly;
using Xunit;

namespace JoinRpg.BlobStorage;

/// <summary>
/// #5526: в БД хранится ключ объекта, публичный адрес собирается из настроек.
/// </summary>
public class AvatarUriResolverTests
{
    private const string Original = "https://provider.example/avatar.jpg";
    private const string Key = "ABCDEF.jpg";

    [Theory]
    [InlineData("https://avatars.joinrpg.ru")]
    [InlineData("https://avatars.joinrpg.ru/")]
    public void KeyIsAppendedToPublicBaseUrl(string publicBaseUrl)
    {
        var options = new S3StorageOptions { PublicBaseUrl = publicBaseUrl, Endpoint = "https://s3.example", BucketName = "bucket" };

        AvatarUriResolver.Resolve(options, Key, Original)
            .ShouldBe(new Uri("https://avatars.joinrpg.ru/ABCDEF.jpg"));
    }

    [Theory]
    [InlineData("https://s3.example")]
    [InlineData("https://s3.example/")]
    public void WithoutPublicBaseUrlKeyIsAppendedToEndpointAndBucket(string endpoint)
    {
        var options = new S3StorageOptions { Endpoint = endpoint, BucketName = "bucket" };

        AvatarUriResolver.Resolve(options, Key, Original)
            .ShouldBe(new Uri("https://s3.example/bucket/ABCDEF.jpg"));
    }

    [Fact]
    public void LegacyAbsoluteUriIsReturnedAsIs()
    {
        var options = new S3StorageOptions { PublicBaseUrl = "https://avatars.joinrpg.ru" };

        AvatarUriResolver.Resolve(options, "https://storage.yandexcloud.net/bucket/ABCDEF.jpg", Original)
            .ShouldBe(new Uri("https://storage.yandexcloud.net/bucket/ABCDEF.jpg"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void NotCachedFallsBackToOriginal(string? cachedUri)
    {
        var options = new S3StorageOptions { PublicBaseUrl = "https://avatars.joinrpg.ru" };

        AvatarUriResolver.Resolve(options, cachedUri, Original).ShouldBe(new Uri(Original));
    }

    [Fact]
    public void NoStorageConfiguredFallsBackToOriginal()
    {
        AvatarUriResolver.Resolve(new S3StorageOptions(), Key, Original).ShouldBe(new Uri(Original));
    }

    [Fact]
    public void EndpointWithoutBucketFallsBackToOriginal()
    {
        var options = new S3StorageOptions { Endpoint = "https://s3.example" };

        AvatarUriResolver.Resolve(options, Key, Original).ShouldBe(new Uri(Original));
    }
}
