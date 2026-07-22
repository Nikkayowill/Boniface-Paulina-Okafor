using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Okafor_.NET.Services;
using System.IO.Compression;

namespace Okafor_.NET.Tests;

public class PatientDocumentStorageServiceTests
{
    [Fact]
    public async Task SaveAsync_StoresPatientDocumentsOutsideWebRoot()
    {
        var contentRoot = CreateTempDirectory();
        var webRoot = CreateTempDirectory();

        try
        {
            var service = CreateService(contentRoot, webRoot);
            var file = CreateFormFile("%PDF-1.7 test"u8.ToArray(), "results.pdf", "application/pdf");

            var stored = await service.SaveAsync(file, PatientDocumentUploadPolicy.Patient);

            Assert.StartsWith("patient-documents/", stored.StorageKey);
            Assert.False(stored.StorageKey.StartsWith('/'));
            Assert.False(File.Exists(Path.Combine(webRoot, stored.StorageKey)));

            var savedFileName = Path.GetFileName(stored.StorageKey);
            Assert.True(File.Exists(Path.Combine(contentRoot, "App_Data", "patient-documents", savedFileName)));
            Assert.Empty(Directory.GetFiles(
                Path.Combine(contentRoot, "App_Data", "patient-documents"),
                "*.uploading"));
        }
        finally
        {
            DeleteDirectory(contentRoot);
            DeleteDirectory(webRoot);
        }
    }

    [Fact]
    public void Constructor_RejectsPatientStorageInsidePublicWebRoot()
    {
        var contentRoot = CreateTempDirectory();
        var webRoot = CreateTempDirectory();

        try
        {
            var action = () => CreateService(
                contentRoot,
                webRoot,
                new PatientDocumentStorageOptions
                {
                    StorageRoot = Path.Combine(webRoot, "patient-documents")
                });

            var exception = Assert.Throws<InvalidOperationException>(action);
            Assert.Contains("outside the public web root", exception.Message);
        }
        finally
        {
            DeleteDirectory(contentRoot);
            DeleteDirectory(webRoot);
        }
    }

    [Fact]
    public async Task HealthCheck_ProvesEnabledStorageIsWritableAndLeavesNoProbeFile()
    {
        var contentRoot = CreateTempDirectory();
        var webRoot = CreateTempDirectory();
        var storageRoot = Path.Combine(contentRoot, "persistent", "patient-documents");

        try
        {
            var healthCheck = new PatientDocumentStorageHealthCheck(
                new EnabledDocumentFeatures(),
                new TestWebHostEnvironment
                {
                    ContentRootPath = contentRoot,
                    WebRootPath = webRoot
                },
                Options.Create(new PatientDocumentStorageOptions
                {
                    StorageRoot = storageRoot,
                    PersistentStorageConfirmed = true
                }));

            var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

            Assert.Equal(HealthStatus.Healthy, result.Status);
            Assert.Empty(Directory.GetFiles(storageRoot));
        }
        finally
        {
            DeleteDirectory(contentRoot);
            DeleteDirectory(webRoot);
        }
    }

    [Fact]
    public void Validate_RejectsFilesWhoseContentDoesNotMatchExtension()
    {
        var contentRoot = CreateTempDirectory();
        var webRoot = CreateTempDirectory();

        try
        {
            var service = CreateService(contentRoot, webRoot);
            var file = CreateFormFile("not actually a pdf"u8.ToArray(), "results.pdf", "application/pdf");

            var result = service.Validate(file, PatientDocumentUploadPolicy.Patient);

            Assert.False(result.IsValid);
            Assert.Equal("The uploaded file content does not match the file type.", result.ErrorMessage);
        }
        finally
        {
            DeleteDirectory(contentRoot);
            DeleteDirectory(webRoot);
        }
    }

    [Fact]
    public void Validate_KeepsAdminDocumentPolicyToPdfAndImages()
    {
        var contentRoot = CreateTempDirectory();
        var webRoot = CreateTempDirectory();

        try
        {
            var service = CreateService(contentRoot, webRoot);
            var file = CreateFormFile("PK docx"u8.ToArray(), "letter.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

            var result = service.Validate(file, PatientDocumentUploadPolicy.Admin);

            Assert.False(result.IsValid);
            Assert.Equal("Only PDF, JPG, JPEG, PNG, and WebP files are allowed.", result.ErrorMessage);
        }
        finally
        {
            DeleteDirectory(contentRoot);
            DeleteDirectory(webRoot);
        }
    }

    [Fact]
    public void Validate_RejectsOversizedFilesBeforeReadingContent()
    {
        var contentRoot = CreateTempDirectory();
        var webRoot = CreateTempDirectory();

        try
        {
            var service = CreateService(contentRoot, webRoot);
            var file = new FormFile(Stream.Null, 0, (10 * 1024 * 1024) + 1, "file", "results.pdf")
            {
                Headers = new HeaderDictionary
                {
                    ["Content-Type"] = "application/pdf"
                }
            };

            var result = service.Validate(file, PatientDocumentUploadPolicy.Patient);

            Assert.False(result.IsValid);
            Assert.Equal("File size must not exceed 10 MB.", result.ErrorMessage);
        }
        finally
        {
            DeleteDirectory(contentRoot);
            DeleteDirectory(webRoot);
        }
    }

    [Fact]
    public void Validate_RejectsZipArchiveRenamedAsDocx()
    {
        var contentRoot = CreateTempDirectory();
        var webRoot = CreateTempDirectory();

        try
        {
            var service = CreateService(contentRoot, webRoot);
            var file = CreateFormFile(CreateZipArchive("unrelated.txt"), "results.docx",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

            var result = service.Validate(file, PatientDocumentUploadPolicy.Patient);

            Assert.False(result.IsValid);
            Assert.Equal("The uploaded file content does not match the file type.", result.ErrorMessage);
        }
        finally
        {
            DeleteDirectory(contentRoot);
            DeleteDirectory(webRoot);
        }
    }

    [Fact]
    public void Validate_AcceptsDocxWithRequiredPackageEntries()
    {
        var contentRoot = CreateTempDirectory();
        var webRoot = CreateTempDirectory();

        try
        {
            var service = CreateService(contentRoot, webRoot);
            var file = CreateFormFile(CreateZipArchive("[Content_Types].xml", "word/document.xml"), "results.docx",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

            var result = service.Validate(file, PatientDocumentUploadPolicy.Patient);

            Assert.True(result.IsValid);
            Assert.Null(result.ErrorMessage);
        }
        finally
        {
            DeleteDirectory(contentRoot);
            DeleteDirectory(webRoot);
        }
    }

    private static PatientDocumentStorageService CreateService(
        string contentRoot,
        string webRoot,
        PatientDocumentStorageOptions? options = null)
    {
        var environment = new TestWebHostEnvironment
        {
            ContentRootPath = contentRoot,
            WebRootPath = webRoot
        };

        return new PatientDocumentStorageService(
            environment,
            Options.Create(options ?? new PatientDocumentStorageOptions()),
            NullLogger<PatientDocumentStorageService>.Instance);
    }

    private static FormFile CreateFormFile(byte[] content, string fileName, string contentType)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, content.Length, "file", fileName)
        {
            Headers = new HeaderDictionary
            {
                ["Content-Type"] = contentType
            }
        };
    }

    private static byte[] CreateZipArchive(params string[] entryNames)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var entryName in entryNames)
            {
                var entry = archive.CreateEntry(entryName);
                using var writer = new StreamWriter(entry.Open());
                writer.Write("test content");
            }
        }

        return stream.ToArray();
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"okafor-docs-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void DeleteDirectory(string directory)
    {
        if (Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
    }

    private sealed class EnabledDocumentFeatures : ILaunchFeatureAvailability
    {
        public bool IsEnabled(LaunchFeature feature) => feature == LaunchFeature.PatientDocuments;
    }
}
