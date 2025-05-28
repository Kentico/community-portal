using Kentico.Community.Portal.Web.Infrastructure.Storage;
using Kentico.Xperience.Cloud;
using Microsoft.AspNetCore.Hosting;
using static Kentico.Community.Portal.Core.Infrastructure.IStoragePathService;

namespace Kentico.Community.Portal.Web.Tests.Infrastructure.Storage;

public class StoragePathServiceTests
{
    [Test]
    public void ShouldMapAzureStorage_Will_Return_False_For_Local_Development()
    {
        var env = Substitute.For<IWebHostEnvironment>();
        env.EnvironmentName = "Development";

        var sut = new StoragePathService(env);

        Assert.That(sut.ShouldMapAzureStorage, Is.False);
    }

    [Test]
    public void ShouldMapAzureStorage_Will_Return_True_For_SaaS_Environments()
    {
        var envQA = Substitute.For<IWebHostEnvironment>();
        envQA.EnvironmentName = CloudEnvironments.Qa;

        var sut = new StoragePathService(envQA);
        Assert.That(sut.ShouldMapAzureStorage, Is.True);

        var envUAT = Substitute.For<IWebHostEnvironment>();
        envUAT.EnvironmentName = CloudEnvironments.Uat;

        sut = new StoragePathService(envUAT);
        Assert.That(sut.ShouldMapAzureStorage, Is.True);

        var envProd = Substitute.For<IWebHostEnvironment>();
        envProd.EnvironmentName = "Production";

        sut = new StoragePathService(envProd);
        Assert.That(sut.ShouldMapAzureStorage, Is.True);
    }

    [TestCase(StorageAssetType.Xperience, "~/assets/")]
    [TestCase(StorageAssetType.Member, "~/member-assets/")]
    public void GetStoragePathPrefix_Will_Return_A_Custom_Prefix_For_Each_AssetType(StorageAssetType assetType, string expectedResult)
    {
        var env = Substitute.For<IWebHostEnvironment>();
        env.EnvironmentName = "Development";

        var sut = new StoragePathService(env);
        string prefix = sut.GetStoragePathPrefix(assetType);

        Assert.That(prefix, Is.EqualTo(expectedResult));
    }

    [Test]
    public void GetStorageFilePath_Will_Return_A_Custom_Path_For_Member_Assets()
    {
        var env = Substitute.For<IWebHostEnvironment>();
        env.EnvironmentName = "Development";

        string relativeFilePath = $"test-path{Path.DirectorySeparatorChar}test-file.png";

        var sut = new StoragePathService(env);
        string fullFilePath = sut.GetStorageFilePath(relativeFilePath, StorageAssetType.Member);

        Assert.That(fullFilePath, Is.EqualTo($"member-assets{Path.DirectorySeparatorChar}{relativeFilePath}"));
    }

    [Test]
    public void GetStorageFilePath_Will_Throw_For_Xperience_Assets()
    {
        var env = Substitute.For<IWebHostEnvironment>();
        env.EnvironmentName = "Development";

        string filePath = "test-path/test-file.png";

        var sut = new StoragePathService(env);
        _ = Assert.Throws<ArgumentOutOfRangeException>(() => sut.GetStorageFilePath(filePath, StorageAssetType.Xperience));
    }

    public static IEnumerable<object[]> GetContainerPath_Will_Return_A_Custom_Path_For_Each_AssetType_For_Local_Development_Source =
    [
        [StorageAssetType.Xperience, $"$StorageAssets{Path.DirectorySeparatorChar}default"],
        [StorageAssetType.Member, $"$StorageMemberAssets{Path.DirectorySeparatorChar}default"]
    ];

    [TestCaseSource(nameof(GetContainerPath_Will_Return_A_Custom_Path_For_Each_AssetType_For_Local_Development_Source))]
    public void GetContainerPath_Will_Return_A_Custom_Path_For_Each_AssetType_For_Local_Development(StorageAssetType assetType, string expectedResult)
    {
        var env = Substitute.For<IWebHostEnvironment>();
        env.EnvironmentName = "Development";

        var sut = new StoragePathService(env);
        string containerPath = sut.GetContainerPath(assetType);

        Assert.That(containerPath, Is.EqualTo(expectedResult));
    }

    [TestCase(StorageAssetType.Xperience, "default")]
    [TestCase(StorageAssetType.Member, "default")]
    public void GetContainerPath_Will_Return_The_Same_Path_For_Each_AssetType_For_SaaS_Deployments(StorageAssetType assetType, string expectedResult)
    {
        var env = Substitute.For<IWebHostEnvironment>();
        env.EnvironmentName = "Production";

        var sut = new StoragePathService(env);
        string containerPath = sut.GetContainerPath(assetType);

        Assert.That(containerPath, Is.EqualTo(expectedResult));
    }
}

