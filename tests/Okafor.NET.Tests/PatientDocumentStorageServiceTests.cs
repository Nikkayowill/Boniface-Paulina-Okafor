using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Okafor_.NET.Services;

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

    private static PatientDocumentStorageService CreateService(string contentRoot, string webRoot)
    {
        var environment = new TestWebHostEnvironment
        {
            ContentRootPath = contentRoot,
            WebRootPath = webRoot
        };

        return new PatientDocumentStorageService(
            environment,
            Options.Create(new PatientDocumentStorageOptions()),
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
}
