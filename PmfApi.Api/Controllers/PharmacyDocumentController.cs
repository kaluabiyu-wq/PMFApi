using Microsoft.AspNetCore.Mvc;
using PmfApi.Application.Interfaces;
using PmfApi.Application.Dtos;

namespace PmfApi.Api.Controllers;

[ApiController]
[Route("api/pharmacies/{pharmacyId:int}/documents")]
[Tags("Pharmacy Documents")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class PharmacyDocumentController(
    IPharmacyDocumentService pharmacyDocumentService,
    IPharmaciesService pharmaciesService) : ControllerBase
{
    [HttpGet(Name = nameof(GetDocumentsByPharmacy))]
    [ProducesResponseType(typeof(IReadOnlyList<PharmacyDocumentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("List the verification documents on file for a pharmacy")]
    [EndpointDescription("Returns every document uploaded for the pharmacy (licenses, business registration, pharmacist credentials), most recent first. Returns 404 if the pharmacy does not exist.")]
    public async Task<IActionResult> GetDocumentsByPharmacy(int pharmacyId, CancellationToken ct)
    {
        var notFound = await CheckPharmacyExistsAsync(pharmacyId, ct);
        if (notFound is not null) return notFound;

        var documents = await pharmacyDocumentService.GetByPharmacyAsync(pharmacyId, ct);
        return Ok(documents);
    }

    [HttpGet("{id:int}", Name = nameof(GetDocumentById))]
    [ProducesResponseType(typeof(PharmacyDocumentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get a single verification document")]
    [EndpointDescription("Returns a single document scoped to the parent pharmacy. Returns 404 if the pharmacy or the document does not exist.")]
    public async Task<IActionResult> GetDocumentById(int pharmacyId, int id, CancellationToken ct)
    {
        var notFound = await CheckPharmacyExistsAsync(pharmacyId, ct);
        if (notFound is not null) return notFound;

        var document = await pharmacyDocumentService.GetByPharmacyIdAsync(pharmacyId, id, ct);
        return document is not null ? Ok(document) : NotFound();
    }

    [HttpPost(Name = nameof(CreateDocument))]
    [ProducesResponseType(typeof(PharmacyDocumentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Upload a verification document for a pharmacy")]
    [EndpointDescription("Creates a document record (License, BusinessRegistration, or PharmacistCredential) for the pharmacy with ReviewStatus starting at Pending. Returns 404 if the pharmacy does not exist.")]
    public async Task<IActionResult> CreateDocument(int pharmacyId, PharmacyDocumentRequest request, CancellationToken ct)
    {
        var notFound = await CheckPharmacyExistsAsync(pharmacyId, ct);
        if (notFound is not null) return notFound;

        var result = await pharmacyDocumentService.CreateAsync(pharmacyId, request, ct);
        return CreatedAtAction(nameof(GetDocumentById), new { pharmacyId, id = result.Id }, result);
    }

    [HttpPatch("{id:int}/review", Name = nameof(Review))]
    [ProducesResponseType(typeof(PharmacyDocumentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Approve or reject a pharmacy's document")]
    [EndpointDescription("Records the SystemAdmin who reviewed the document and sets ReviewStatus to Approved or Rejected. Returns 404 if the pharmacy or the document does not exist.")]
    public async Task<IActionResult> Review(int pharmacyId, int id, PharmacyDocumentReviewRequest request, CancellationToken ct)
    {
        var notFound = await CheckPharmacyExistsAsync(pharmacyId, ct);
        if (notFound is not null) return notFound;

        var result = await pharmacyDocumentService.ReviewAsync(pharmacyId, id, request, ct);
        return result is not null ? Ok(result) : NotFound();
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
