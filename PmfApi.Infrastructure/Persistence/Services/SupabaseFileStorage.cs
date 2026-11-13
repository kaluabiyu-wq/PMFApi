using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using PmfApi.Application.Exceptions;
using PmfApi.Application.Interfaces;

namespace PmfApi.Infrastructure.Persistence.Services;

public class SupabaseFileStorage : IFileStorage
{
    private const long MaxBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".jpg", ".jpeg", ".png" };

    private readonly HttpClient _http;
    private readonly string _bucket;

    public SupabaseFileStorage(HttpClient http, IConfiguration config)
    {
        var baseUrl = config["Supabase:Url"]?.TrimEnd('/')
            ?? throw new InvalidOperationException("Supabase:Url is missing.");
        var key = config["Supabase:ServiceKey"]
            ?? throw new InvalidOperationException("Supabase:ServiceKey is missing.");

        _bucket = config["Supabase:Bucket"] ?? "pmf-uploads";
        _http = http;
        _http.BaseAddress = new Uri(baseUrl + "/storage/v1/");
        _http.DefaultRequestHeaders.Add("apikey", key);
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);
    }

    public async Task<string> SaveAsync(IFormFile file, string folder, CancellationToken ct)
    {
        var ext = Path.GetExtension(file.FileName);
        if (file.Length == 0 || file.Length > MaxBytes || !AllowedExtensions.Contains(ext))
            throw new InvalidUploadException(
                $"'{file.FileName}' must be a non-empty .pdf/.jpg/.png file up to 5 MB.");

        var objectPath = $"{folder}/{Guid.NewGuid():N}{ext.ToLowerInvariant()}";

        await using var stream = file.OpenReadStream();
        using var content = new StreamContent(stream);
        content.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);

        using var response = await _http.PostAsync($"object/{_bucket}/{objectPath}", content, ct);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(
                $"Supabase Storage upload failed ({(int)response.StatusCode}): {detail}");
        }

        return objectPath;
    }

    public void Delete(string relativePath)
    {
        if (relativePath.Contains("..")) return;
        try
        {
            using var response = _http.DeleteAsync($"object/{_bucket}/{relativePath}")
                .GetAwaiter().GetResult();
        }
        catch { /* a missing or already-deleted object must not break the caller */ }
    }

    public Stream? OpenRead(string relativePath)
    {
        if (relativePath.Contains("..")) return null;

        using var response = _http.GetAsync($"object/{_bucket}/{relativePath}")
            .GetAwaiter().GetResult();
        if (!response.IsSuccessStatusCode) return null;

        var bytes = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
        return new MemoryStream(bytes);
    }
}
