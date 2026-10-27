using System.IO.Compression;

namespace ReistersDaughter.Api.Services;

public sealed class ResumeStorageService
{
    public const long MaxFileSize = 5 * 1024 * 1024;

    private readonly string _storageDirectory;

    public ResumeStorageService(IConfiguration configuration)
    {
        _storageDirectory = configuration["ResumeStorage:Directory"]
            ?? throw new InvalidOperationException(
                "ResumeStorage:Directory is not configured.");

        if (!Path.IsPathFullyQualified(_storageDirectory))
            throw new InvalidOperationException(
                "Resume storage must use an absolute directory path.");

        Directory.CreateDirectory(_storageDirectory);
    }

    public async Task<(string Key, string OriginalName)> SaveAsync(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file.Length <= 0 || file.Length > MaxFileSize)
            throw new InvalidDataException(
                "Résumé must be between 1 byte and 5 MB.");

        var originalName = Path.GetFileName(file.FileName);

        if (string.IsNullOrWhiteSpace(originalName) ||
            originalName.Length > 255 ||
            originalName.Any(char.IsControl) ||
            originalName.Contains('/') ||
            originalName.Contains('\\'))
            throw new InvalidDataException("Invalid résumé filename.");

        var extension = Path.GetExtension(originalName).ToLowerInvariant();

        if (extension is not (".pdf" or ".doc" or ".docx"))
            throw new InvalidDataException(
                "Only PDF, DOC, and DOCX résumés are supported.");

        await using var input = file.OpenReadStream();
        using var buffer = new MemoryStream();

        await input.CopyToAsync(buffer, cancellationToken);

        if (buffer.Length == 0 || buffer.Length > MaxFileSize)
            throw new InvalidDataException("Invalid résumé file size.");

        var data = buffer.ToArray();

        if (!HasValidSignature(data, extension))
            throw new InvalidDataException(
                "The résumé does not match its declared file type.");

        var key = $"{Guid.NewGuid():N}{extension}";
        var destination = Path.Combine(_storageDirectory, key);

        await using (var output = new FileStream(
            destination,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None))
        {
            await output.WriteAsync(data, cancellationToken);
        }

        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(
                destination,
                UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }

        return (key, originalName);
    }

    public string GetPath(string key)
    {
        if (string.IsNullOrWhiteSpace(key) ||
            key != Path.GetFileName(key) ||
            !Guid.TryParseExact(
                Path.GetFileNameWithoutExtension(key),
                "N",
                out _) ||
            Path.GetExtension(key).ToLowerInvariant()
                is not (".pdf" or ".doc" or ".docx"))
        {
            throw new InvalidDataException("Invalid résumé storage key.");
        }

        return Path.Combine(_storageDirectory, key);
    }

    public void Delete(string key)
    {
        var path = GetPath(key);

        if (File.Exists(path))
            File.Delete(path);
    }

    private static bool HasValidSignature(byte[] data, string extension)
    {
        if (extension == ".pdf")
        {
            if (data.Length < 8 ||
                !data.AsSpan(0, 5).SequenceEqual("%PDF-"u8))
                return false;

            // Require a PDF EOF marker near the end of the document.
            var tail = data.AsSpan(Math.Max(0, data.Length - 2048));
            return tail.IndexOf("%%EOF"u8) >= 0;
        }

        if (extension == ".doc")
        {
            byte[] signature =
                [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];

            return data.Length >= 512 &&
                data.AsSpan(0, signature.Length).SequenceEqual(signature);
        }

        if (extension == ".docx")
        {
            try
            {
                using var stream = new MemoryStream(data);
                using var archive = new ZipArchive(
                    stream, ZipArchiveMode.Read);

                if (archive.Entries.Count > 2000)
                    return false;

                long expandedSize = 0;

                foreach (var entry in archive.Entries)
                {
                    expandedSize += entry.Length;

                    if (expandedSize > 50L * 1024 * 1024)
                        return false;

                    if (entry.FullName.StartsWith("/") ||
                        entry.FullName.Contains('\\') ||
                        entry.FullName.Split('/').Contains(".."))
                        return false;

                    if (entry.FullName.EndsWith(".bin",
                        StringComparison.OrdinalIgnoreCase) &&
                        entry.FullName.Contains("vbaProject",
                        StringComparison.OrdinalIgnoreCase))
                        return false;
                }

                return archive.GetEntry("[Content_Types].xml") != null &&
                    archive.GetEntry("word/document.xml") != null &&
                    archive.GetEntry("_rels/.rels") != null;
            }
            catch (InvalidDataException)
            {
                return false;
            }
        }

        return false;
    }
}
