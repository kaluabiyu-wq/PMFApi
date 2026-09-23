using Microsoft.EntityFrameworkCore;
using PmfApi.Infrastructure.Persistence;
using PmfApi.Application.Dtos;
using PmfApi.Domain.Entities;
using PmfApi.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace PmfApi.Infrastructure.Persistence.Services;

public class ReviewService(PmfDbContext context, ILogger<ReviewService> logger)
: IReviewService
{
    public async Task<ReviewResponse> CreateAsync(int pharmacyId, ReviewRequest request, CancellationToken ct)
    {
        var review = new Review
        {
            PharmacyId = pharmacyId,
            UserId = request.UserId,
            Rating = request.Rating,
            Comment = request.Comment,
            SubmittedAt = DateTime.UtcNow,
        };

        context.Reviews.Add(review);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Created Review {ReviewId} for Pharmacy {PharmacyId} by User {UserId} with Rating {Rating}",
            review.Id, review.PharmacyId, review.UserId, review.Rating);

        return (await GetByPharmacyIdAsync(review.PharmacyId, review.Id, ct))!;
    }

    public Task<ReviewResponse?> GetByPharmacyIdAsync(int pharmacyId, int id, CancellationToken ct) =>
    context.Reviews.AsNoTracking()
    .Where(r => r.Id == id && r.PharmacyId == pharmacyId)
    .Select(r => new ReviewResponse(
        r.Id, r.UserId, r.PharmacyId, r.Rating, r.Comment, r.SubmittedAt
    )).FirstOrDefaultAsync(ct);

    public async Task<PagedResponse<ReviewResponse>> GetByPharmacyAsync(int pharmacyId, PagedRequest request, CancellationToken ct)
    {
        IQueryable<Review> query = context.Reviews.AsNoTracking()
            .Where(r => r.PharmacyId == pharmacyId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(r => r.Comment != null && EF.Functions.ILike(r.Comment, $"%{request.Search}%"));
        }

        var totalCount = await query.CountAsync(ct);

        IOrderedQueryable<Review> sortedQuery = request.OrderBy switch
        {
            "Rating" => request.Descending
                ? query.OrderByDescending(r => r.Rating)
                : query.OrderBy(r => r.Rating),
            _ => request.Descending
                ? query.OrderByDescending(r => r.SubmittedAt)
                : query.OrderBy(r => r.SubmittedAt)
        };

        var items = await sortedQuery
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new ReviewResponse(r.Id, r.UserId, r.PharmacyId, r.Rating, r.Comment, r.SubmittedAt))
            .ToListAsync(ct);

        return new PagedResponse<ReviewResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public Task<double?> GetAverageRatingAsync(int pharmacyId, CancellationToken ct) =>
    context.Reviews.AsNoTracking()
    .Where(r => r.PharmacyId == pharmacyId)
    .Select(r => (double?)r.Rating)
    .AverageAsync(ct);
}
