using Microsoft.AspNetCore.Http;

namespace PmfApi.Application.Interfaces;

public interface IFileStorage
{
    /// <summary>Validates and saves the file. Returns the stored relative path.</summary>
    Task<string> SaveAsync(IFormFile file, string folder, CancellationToken ct);

    void Delete(string relativePath);
}