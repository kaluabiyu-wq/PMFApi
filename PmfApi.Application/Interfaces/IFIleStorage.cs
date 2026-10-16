using Microsoft.AspNetCore.Http;

namespace PmfApi.Application.Interfaces;

public interface IFileStorage
{
    
    Task<string> SaveAsync(IFormFile file, string folder, CancellationToken ct);

    void Delete(string relativePath);

    Stream? OpenRead(string relativePath);
}