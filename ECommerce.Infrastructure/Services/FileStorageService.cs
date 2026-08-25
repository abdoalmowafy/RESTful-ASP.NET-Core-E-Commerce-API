using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Services;

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";
    public string RootPath { get; set; } = "wwwroot";
}

public sealed record StoredFile(string Url, long SizeBytes, string Extension);

/// <summary>
/// Central local-disk storage for user uploads. Enforces per-file size caps and
/// extension whitelists in one place; swap for blob storage behind this interface later.
/// </summary>
public interface IFileStorage
{
    /// <summary>Throws InvalidOperationException on unsupported/empty files.</summary>
    Task<IReadOnlyList<StoredFile>> SaveAllAsync(
        IEnumerable<IFormFile> files,
        string folder,
        long maxBytesPerFile,
        IReadOnlyCollection<string> allowedExtensions,
        CancellationToken cancellationToken = default);

    string? TryGetInvalidReason(IFormFile file, long maxBytesPerFile, IReadOnlyCollection<string> allowedExtensions);
}

public class LocalFileStorage(IWebHostEnvironment environment, IOptions<FileStorageOptions> options) : IFileStorage
{
    private readonly FileStorageOptions _options = options.Value;
    private readonly IWebHostEnvironment _environment = environment;

    public string? TryGetInvalidReason(IFormFile file, long maxBytesPerFile, IReadOnlyCollection<string> allowedExtensions)
    {
        if (file is null || file.Length == 0)
            return "Empty file";

        if (file.Length > maxBytesPerFile)
            return $"File exceeds the {maxBytesPerFile / (1024 * 1024)} MB limit";

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
            return $"Unsupported file type '{extension}'";

        return null;
    }

    public async Task<IReadOnlyList<StoredFile>> SaveAllAsync(
        IEnumerable<IFormFile> files,
        string folder,
        long maxBytesPerFile,
        IReadOnlyCollection<string> allowedExtensions,
        CancellationToken cancellationToken = default)
    {
        var root = _options.RootPath;
        if (!Path.IsPathRooted(root))
            root = Path.Combine(_environment.ContentRootPath, root);

        var directory = Path.Combine(root, folder.Replace('/', Path.DirectorySeparatorChar).TrimStart('/'));
        Directory.CreateDirectory(directory);

        var saved = new List<StoredFile>();

        foreach (var file in files)
        {
            var invalid = TryGetInvalidReason(file, maxBytesPerFile, allowedExtensions);
            if (invalid is not null)
                throw new InvalidOperationException(invalid);

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var physicalPath = Path.Combine(directory, fileName);

            await using var stream = File.Create(physicalPath);
            await file.CopyToAsync(stream, cancellationToken);

            saved.Add(new StoredFile($"/{folder.TrimEnd('/')}/{fileName}", file.Length, extension));
        }

        return saved;
    }
}
