using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Okafor_.NET.Services;

namespace Okafor_.NET.Tests;

public sealed class ImageServiceTests : IDisposable
{
    private readonly string _rootPath = Path.Combine(Path.GetTempPath(), "OkaforImageServiceTests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void GetRandomHospitalImage_ReturnsDefaultWhenNoImagesExist()
    {
        var service = CreateService();

        var result = service.GetRandomHospitalImage();

        Assert.Equal("/images/placeholders/placeholder.svg", result);
    }

    [Fact]
    public void GetRandomHospitalImages_ReturnsRequestedImagesWhenFilesExist()
    {
        var imageDirectory = Path.Combine(_rootPath, "images", "placeholders", "Hospital");
        Directory.CreateDirectory(imageDirectory);
        File.WriteAllText(Path.Combine(imageDirectory, "one.webp"), "1");
        File.WriteAllText(Path.Combine(imageDirectory, "two.webp"), "2");

        var service = CreateService();

        var results = service.GetRandomHospitalImages(2);

        Assert.Equal(2, results.Count);
        Assert.All(results, result => Assert.StartsWith("/images/placeholders/Hospital/", result));
        Assert.Contains(results, result => result.EndsWith("one.webp", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(results, result => result.EndsWith("two.webp", StringComparison.OrdinalIgnoreCase));
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }

    private ImageService CreateService()
    {
        Directory.CreateDirectory(_rootPath);

        var environment = new TestWebHostEnvironment
        {
            ApplicationName = "Okafor-.NET",
            ContentRootPath = _rootPath,
            ContentRootFileProvider = new PhysicalFileProvider(_rootPath),
            EnvironmentName = Environments.Development,
            WebRootPath = _rootPath,
            WebRootFileProvider = new PhysicalFileProvider(_rootPath)
        };

        return new ImageService(environment, NullLogger<ImageService>.Instance);
    }
}
