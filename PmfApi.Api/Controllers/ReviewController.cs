
using Microsoft.AspNetCore.Mvc;
using PmfApi.Application.Interfaces;
using PmfApi.Application.Dtos;

namespace PmfApi.Api.Controllers;

[ApiController]
[Route("api/pharmacies/{pharmacyId:int}/reviews")]
[Tags("Reviews")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class ReviewController(
    IReviewService reviewService,
    IPharmaciesService pharmaciesService) : ControllerBase
{
    [HttpGet(Name = nameof(GetReviewsByPharmacy))]
    [ProducesResponseType(typeof(PagedResponse<ReviewResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("List a pharmacy's service reviews")]
    [EndpointDescription("Returns a paginated list of general service-experience reviews (staff, wait time, honored pricing) for the pharmacy. Returns 404 if the pharmacy does not exist.")]
    public async Task<IActionResult> GetReviewsByPharmacy(int pharmacyId, [FromQuery] PagedRequest request, CancellationToken ct)
    {
        var notFound = await CheckPharmacyExistsAsync(pharmacyId, ct);
        if (notFound is not null) return notFound;

        var reviews = await reviewService.GetByPharmacyAsync(pharmacyId, request, ct);
        return Ok(reviews);
    }

    [HttpGet("{id:int}", Name = nameof(GetReviewById))]
    [ProducesResponseType(typeof(ReviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get a single review")]
    [EndpointDescription("Returns a single review scoped to the parent pharmacy. Returns 404 if the pharmacy or the review does not exist.")]
    public async Task<IActionResult> GetReviewById(int pharmacyId, int id, CancellationToken ct)
    {
        var notFound = await CheckPharmacyExistsAsync(pharmacyId, ct);
        if (notFound is not null) return notFound;

        var review = await reviewService.GetByPharmacyIdAsync(pharmacyId, id, ct);
        return review is not null ? Ok(review) : NotFound();
    }

    [HttpGet("average-rating", Name = nameof(GetAverageRating))]
    [ProducesResponseType(typeof(double?), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get a pharmacy's average service rating")]
    [EndpointDescription("Returns the mean Rating (1-5) across all reviews for the pharmacy, or null if it has none. This is service-experience only and is never factored into Pharmacy.ReliablityScore.")]
    public async Task<IActionResult> GetAverageRating(int pharmacyId, CancellationToken ct)
    {
        var notFound = await CheckPharmacyExistsAsync(pharmacyId, ct);
        if (notFound is not null) return notFound;

        var average = await reviewService.GetAverageRatingAsync(pharmacyId, ct);
        return Ok(average);
    }

    [HttpPost(Name = nameof(CreateReview))]
    [ProducesResponseType(typeof(ReviewResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Submit a service review for a pharmacy")]
    [EndpointDescription("Creates a 1-5 star service-experience review, with an optional comment. Returns 404 if the pharmacy does not exist.")]
    public async Task<IActionResult> CreateReview(int pharmacyId, ReviewRequest request, CancellationToken ct)
    {
        var notFound = await CheckPharmacyExistsAsync(pharmacyId, ct);
        if (notFound is not null) return notFound;

        var result = await reviewService.CreateAsync(pharmacyId, request, ct);
        return CreatedAtAction(nameof(GetReviewById), new { pharmacyId, id = result.Id }, result);
    }

    private async Task<IActionResult?> CheckPharmacyExistsAsync(int pharmacyId, CancellationToken ct)
    {
        var pharmacy = await pharmaciesService.GetByIdAsync(pharmacyId, ct);
        if (pharmacy is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Pharmacy not found",
                Detail = $"No pharmacy exists with id {pharmacyId}.",
                Status = StatusCodes.Status404NotFound,
            });
        }

        return null;
    }
}
