using Microsoft.Extensions.Options;
using System.IO.Compression;

namespace Okafor_.NET.Services;

public enum PatientDocumentUploadPolicy
{
    Patient,
    Admin
}

public sealed class PatientDocumentStorageOptions
{
    public string? StorageRoot { get; set; }
}

public sealed record PatientDocumentValidationResult(bool IsValid, string? ErrorMessage)
{
    public static PatientDocumentValidationResult Success { get; } = new(true, null);

    public static PatientDocumentValidationResult Failure(string errorMessage) => new(false, errorMessage);
}

public sealed record StoredPatientDocument(string StorageKey, string ContentType);

public sealed record PatientDocumentReadResult(Stream Stream, string ContentType, string FileName);

public interface IPatientDocumentStorageService
{
    PatientDocumentValidationResult Validate(IFormFile file, PatientDocumentUploadPolicy policy);
    Task<StoredPatientDocument> SaveAsync(IFormFile file, PatientDocumentUploadPolicy policy, CancellationToken cancellationToken = default);
    Task<PatientDocumentReadResult?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}

public sealed class PatientDocumentStorageService : IPatientDocumentStorageService
{
    private const long MaxFileBytes = 10 * 1024 * 1024;
    private const string StorageFolder = "patient-documents";

    private static readonly HashSet<string> PatientExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx"
    };

    private static readonly HashSet<string> AdminExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png", ".webp"
    };

    private static readonly Dictionary<string, string> ContentTypesByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp",
        [".doc"] = "application/msword",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };

    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<PatientDocumentStorageService> _logger;
    private readonly string _storageRoot;

    public PatientDocumentStorageService(
        IWebHostEnvironment environment,
        IOptions<PatientDocumentStorageOptions> options,
        ILogger<PatientDocumentStorageService> logger)
    {
        _environment = environment;
        _logger = logger;
        _storageRoot = string.IsNullOrWhiteSpace(options.Value.StorageRoot)
            ? Path.Combine(environment.ContentRootPath, "App_Data", "patient-documents")
            : options.Value.StorageRoot;
    }

    public PatientDocumentValidationResult Validate(IFormFile file, PatientDocumentUploadPolicy policy)
    {
        if (file.Length <= 0)
            return PatientDocumentValidationResult.Failure("Please select a file to upload.");

        if (file.Length > MaxFileBytes)
            return PatientDocumentValidationResult.Failure($"File size must not exceed {MaxFileBytes / (1024 * 1024)} MB.");

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions(policy).Contains(extension))
            return PatientDocumentValidationResult.Failure(AllowedFileMessage(policy));

        if (!ContentTypesByExtension.TryGetValue(extension, out var expectedContentType))
            return PatientDocumentValidationResult.Failure(AllowedFileMessage(policy));

        if (!string.Equals(file.ContentType, expectedContentType, StringComparison.OrdinalIgnoreCase))
            return PatientDocumentValidationResult.Failure("Invalid file type.");

        if (!HasExpectedContent(file, extension))
            return PatientDocumentValidationResult.Failure("The uploaded file content does not match the file type.");

        return PatientDocumentValidationResult.Success;
    }

    public async Task<StoredPatientDocument> SaveAsync(
        IFormFile file,
        PatientDocumentUploadPolicy policy,
        CancellationToken cancellationToken = default)
    {
        var validation = Validate(file, policy);
        if (!validation.IsValid)
            throw new InvalidOperationException(validation.ErrorMessage);

        Directory.CreateDirectory(_storageRoot);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(_storageRoot, fileName);

        await using (var stream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        return new StoredPatientDocument($"{StorageFolder}/{fileName}", ContentTypesByExtension[extension]);
    }

    public Task<PatientDocumentReadResult?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePath(storageKey);
        if (fullPath is null || !File.Exists(fullPath))
            return Task.FromResult<PatientDocumentReadResult?>(null);

        var extension = Path.GetExtension(fullPath);
        var contentType = ContentTypesByExtension.GetValueOrDefault(extension, "application/octet-stream");
        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);

        return Task.FromResult<PatientDocumentReadResult?>(new PatientDocumentReadResult(stream, contentType, Path.GetFileName(fullPath)));
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePath(storageKey);
        if (fullPath is null)
            return Task.CompletedTask;

        try
        {
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete patient document file for storage key {StorageKey}", storageKey);
        }

        return Task.CompletedTask;
    }

    private static HashSet<string> AllowedExtensions(PatientDocumentUploadPolicy policy) =>
        policy == PatientDocumentUploadPolicy.Admin ? AdminExtensions : PatientExtensions;

    private static string AllowedFileMessage(PatientDocumentUploadPolicy policy) =>
        policy == PatientDocumentUploadPolicy.Admin
            ? "Only PDF, JPG, JPEG, PNG, and WebP files are allowed."
            : "Only PDF, JPG, JPEG, PNG, DOC, and DOCX files are allowed.";

    private static bool HasExpectedContent(IFormFile file, string extension)
    {
        Span<byte> buffer = stackalloc byte[12];
        using var stream = file.OpenReadStream();
        var bytesRead = stream.Read(buffer);
        var bytes = buffer[..bytesRead];

        return extension.ToLowerInvariant() switch
        {
            ".pdf" => bytes.StartsWith("%PDF"u8),
            ".jpg" or ".jpeg" => bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF,
            ".png" => bytes.StartsWith(stackalloc byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            ".webp" => bytes.Length >= 12 &&
                bytes[..4].SequenceEqual("RIFF"u8) &&
                bytes[8..12].SequenceEqual("WEBP"u8),
            ".doc" => bytes.StartsWith(stackalloc byte[] { 0xD0, 0xCF, 0x11, 0xE0 }),
            ".docx" => bytes.StartsWith("PK"u8) && IsWordDocument(file),
            _ => false
        };
    }

    private static bool IsWordDocument(IFormFile file)
    {
        try
        {
            using var stream = file.OpenReadStream();
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
            return archive.GetEntry("[Content_Types].xml") is not null &&
                archive.GetEntry("word/document.xml") is not null;
        }
        catch (InvalidDataException)
        {
            return false;
        }
    }

    private string? ResolvePath(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            return null;

        var fileName = Path.GetFileName(storageKey);
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        if (storageKey.StartsWith("/uploads/patient-documents/", StringComparison.OrdinalIgnoreCase))
            return Path.Combine(_environment.WebRootPath, "uploads", "patient-documents", fileName);

        return Path.Combine(_storageRoot, fileName);
    }
}
