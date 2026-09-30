using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using PmfApi.Application.Exceptions;
using PmfApi.Application.Interfaces;

namespace PmfApi.Infrastructure.Persistence.Services;

public class LocalFileStorage(IHostEnvironment env) : IFileStorage
{
    private const long MaxBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".jpg", ".jpeg", ".png" };

    // Outside wwwroot on purpose: these are private verification documents.
    private string Root => Path.Combine(env.ContentRootPath, "uploads");

    public async Task<string> SaveAsync(IFormFile file, string folder, CancellationToken ct)
    {
        var ext = Path.GetExtension(file.FileName);
        if (file.Length == 0 || file.Length > MaxBytes || !AllowedExtensions.Contains(ext))
            throw new InvalidUploadException(
                $"'{file.FileName}' must be a non-empty .pdf/.jpg/.png file up to 5 MB.");

        var relative = Path.Combine(folder, $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}");
        var full = Path.Combine(Root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);

        await using var stream = File.Create(full);
        await file.CopyToAsync(stream, ct);
        return relative.Replace('\\', '/');
    }

    public void Delete(string relativePath)
    {
        var full = Path.Combine(Root, relativePath);
        if (File.Exists(full)) File.Delete(full);
    }
}