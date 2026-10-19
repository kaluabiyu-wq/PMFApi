using Microsoft.AspNetCore.Mvc;
using PmfApi.Application.Interfaces;
using PmfApi.Application.Dtos;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.StaticFiles;
using PmfApi.Application.Exceptions;
using PmfApi.Domain.Entities;

namespace PmfApi.Api.Controllers;

[ApiController]
[Route("api/pharmacies/{pharmacyId:int}/documents")]
[Tags("Pharmacy Documents")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class PharmacyDocumentController(
    IPharmacyDocumentService pharmacyDocumentService,
    IPharmaciesService pharmaciesService,
    IAuthorizationService authorizationService,
    IFileStorage fileStorage) : ControllerBase
{
    private static readonly FileExtensionContentTypeProvider ContentTypes = new();

    [HttpGet(Name = nameof(GetDocumentsByPharmacy))]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<PharmacyDocumentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("List the verification documents on file for a pharmacy")]
    [EndpointDescription("Returns every document uploaded for the pharmacy (licenses, business registration, pharmacist credentials), most recent first, including the reviewer's rejection note. Visible to the pharmacy's own staff and to administrators. Returns 404 if the pharmacy does not exist.")]
    public async Task<IActionResult> GetDocumentsByPharmacy(int pharmacyId, CancellationToken ct)
    {
        var pharmacy = await pharmaciesService.GetEntityByIdAsync(pharmacyId, ct);
        if (pharmacy is null) return PharmacyNotFound(pharmacyId);
        if (!await CanViewAsync(pharmacy)) return AccessDenied();

        var documents = await pharmacyDocumentService.GetByPharmacyAsync(pharmacyId, ct);
        return Ok(documents);
    }

    [HttpGet("{id:int}", Name = nameof(GetDocumentById))]
    [Authorize]
    [ProducesResponseType(typeof(PharmacyDocumentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get a single verification document")]
    [EndpointDescription("Returns a single document scoped to the parent pharmacy. Returns 404 if the pharmacy or the document does not exist.")]
    public async Task<IActionResult> GetDocumentById(int pharmacyId, int id, CancellationToken ct)
    {
        var pharmacy = await pharmaciesService.GetEntityByIdAsync(pharmacyId, ct);
        if (pharmacy is null) return PharmacyNotFound(pharmacyId);
        if (!await CanViewAsync(pharmacy)) return AccessDenied();

        var document = await pharmacyDocumentService.GetByPharmacyIdAsync(pharmacyId, id, ct);
        return document is not null ? Ok(document) : NotFound();
    }

    [HttpGet("{id:int}/file", Name = nameof(GetDocumentFile))]
    [Authorize]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK, "application/pdf", "image/jpeg", "image/png")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Download an uploaded verification document")]
    [EndpointDescription("Streams the photo or PDF the pharmacy uploaded. Visible to active staff of that pharmacy and to administrators (SysAdmin, PharmacyAdmin); anyone else gets 403. Returns 404 for a document that was added as a link, since nothing is stored here for those.")]
    public async Task<IActionResult> GetDocumentFile(int pharmacyId, int id, CancellationToken ct)
    {
        var pharmacy = await pharmaciesService.GetEntityByIdAsync(pharmacyId, ct);
        if (pharmacy is null) return PharmacyNotFound(pharmacyId);

        if (!await CanViewAsync(pharmacy)) return AccessDenied();

        var document = await pharmacyDocumentService.GetByPharmacyIdAsync(pharmacyId, id, ct);
        if (document is null) return NotFound();

        var isLink = document.FileUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || document.FileUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        if (isLink)
            return NotFound(new ProblemDetails
            {
                Title = "No stored file",
                Detail = "This document was added as a link, so there is no uploaded file to download.",
                Status = StatusCodes.Status404NotFound,
            });

        var stream = fileStorage.OpenRead(document.FileUrl);
        if (stream is null)
            return NotFound(new ProblemDetails
            {
                Title = "File not found",
                Detail = "The uploaded file is no longer available.",
                Status = StatusCodes.Status404NotFound,
            });

        if (!ContentTypes.TryGetContentType(document.FileUrl, out var contentType))
            contentType = "application/octet-stream";

       
        Response.Headers["Cache-Control"] = "private, no-store";
        return File(stream, contentType);
    }

    [HttpPost(Name = nameof(CreateDocument))]
    [Authorize]
    [ProducesResponseType(typeof(PharmacyDocumentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Add a verification document by link")]
    [EndpointDescription("Creates a document record (License, BusinessRegistration, or PharmacistCredential) pointing at a link, with ReviewStatus starting at Pending. Only the pharmacy's own staff or an administrator may add documents. Returns 404 if the pharmacy does not exist.")]
    public async Task<IActionResult> CreateDocument(int pharmacyId, PharmacyDocumentRequest request, CancellationToken ct)
    {
        var pharmacy = await pharmaciesService.GetEntityByIdAsync(pharmacyId, ct);
        if (pharmacy is null) return PharmacyNotFound(pharmacyId);
        if (!await CanManageAsync(pharmacy)) return AccessDenied();

        var result = await pharmacyDocumentService.CreateAsync(pharmacyId, request, ct);
        return CreatedAtAction(nameof(GetDocumentById), new { pharmacyId, id = result.Id }, result);
    }

    [HttpPost("upload", Name = nameof(UploadDocument))]
    [Authorize]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [ProducesResponseType(typeof(PharmacyDocumentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Upload a new verification document file")]
    [EndpointDescription("Sends the file itself (.pdf, .jpg, .jpeg or .png, up to 5 MB) as multipart/form-data in the 'File' field, with 'DocumentType' and an optional 'ExpiresAt'. The document starts as Pending. Only the pharmacy's own staff or an administrator may upload.")]
    public async Task<IActionResult> UploadDocument(int pharmacyId, [FromForm] PharmacyDocumentUploadRequest request, CancellationToken ct)
    {
        var pharmacy = await pharmaciesService.GetEntityByIdAsync(pharmacyId, ct);
        if (pharmacy is null) return PharmacyNotFound(pharmacyId);
        if (!await CanManageAsync(pharmacy)) return AccessDenied();

        var storedPath = await TrySaveAsync(request.File, ct);
        if (storedPath.Error is not null) return storedPath.Error;

        try
        {
            var result = await pharmacyDocumentService.CreateFromUploadAsync(
                pharmacyId, request.DocumentType, storedPath.Path!, request.ExpiresAt, ct);
            return CreatedAtAction(nameof(GetDocumentById), new { pharmacyId, id = result.Id }, result);
        }
        catch
        {
            fileStorage.Delete(storedPath.Path!);
            throw;
        }
    }

    [HttpPut("{id:int}/file", Name = nameof(ReplaceDocumentFile))]
    [Authorize]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [ProducesResponseType(typeof(PharmacyDocumentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Replace the file of a verification document")]
    [EndpointDescription("Swaps in a new file (.pdf, .jpg, .jpeg or .png, up to 5 MB) and sends the document back to review: ReviewStatus becomes Pending and the reviewer and rejection note are cleared. The old stored file is deleted. Only the pharmacy's own staff or an administrator may do this.")]
    public async Task<IActionResult> ReplaceDocumentFile(int pharmacyId, int id, [FromForm] PharmacyDocumentReplaceRequest request, CancellationToken ct)
    {
        var pharmacy = await pharmaciesService.GetEntityByIdAsync(pharmacyId, ct);
        if (pharmacy is null) return PharmacyNotFound(pharmacyId);
        if (!await CanManageAsync(pharmacy)) return AccessDenied();

        // Check the document exists before anything is written to disk.
        if (await pharmacyDocumentService.GetByPharmacyIdAsync(pharmacyId, id, ct) is null)
            return NotFound();

        var storedPath = await TrySaveAsync(request.File, ct);
        if (storedPath.Error is not null) return storedPath.Error;

        try
        {
            var result = await pharmacyDocumentService.ReplaceFileAsync(
                pharmacyId, id, storedPath.Path!, request.ExpiresAt, ct);

            if (result is null)
            {
                fileStorage.Delete(storedPath.Path!);
                return NotFound();
            }

            return Ok(result);
        }
        catch
        {
            fileStorage.Delete(storedPath.Path!);
            throw;
        }
    }

    [HttpPatch("{id:int}/review", Name = nameof(Review))]
    [Authorize(Policy = "DocumentReview")]
    [ProducesResponseType(typeof(PharmacyDocumentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Approve or reject a pharmacy's document")]
    [EndpointDescription("Records the administrator (SysAdmin or PharmacyAdmin) who reviewed the document and sets ReviewStatus to Approved or Rejected. A Reason is required when rejecting and is sent to the pharmacy's staff as an alert. Returns 404 if the pharmacy or the document does not exist.")]
    public async Task<IActionResult> Review(int pharmacyId, int id, PharmacyDocumentReviewRequest request, CancellationToken ct)
    {
        var notFound = await CheckPharmacyExistsAsync(pharmacyId, ct);
        if (notFound is not null) return notFound;

        // The pharmacy can only fix what it is told is wrong, so a rejection must say why.
        if (request.ReviewStatus == ReviewStatus.Rejected && string.IsNullOrWhiteSpace(request.Reason))
        {
            ModelState.AddModelError(nameof(request.Reason), "Tell the pharmacy why the document was rejected.");
            return ValidationProblem(ModelState);
        }

        var reviewerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var result = await pharmacyDocumentService.ReviewAsync(pharmacyId, id, reviewerId, request, ct);
        return result is not null ? Ok(result) : NotFound();
    }

    private NotFoundObjectResult PharmacyNotFound(int pharmacyId) =>
        NotFound(new ProblemDetails
        {
            Title = "Pharmacy not found",
            Detail = $"No pharmacy exists with id {pharmacyId}.",
            Status = StatusCodes.Status404NotFound,
        });

    private ObjectResult AccessDenied() =>
        StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
        {
            Title = "Access denied",
            Detail = "Only this pharmacy's staff or an administrator can use its documents.",
            Status = StatusCodes.Status403Forbidden,
        });

   
    private async Task<bool> CanViewAsync(Pharmacy pharmacy) =>
        (await authorizationService.AuthorizeAsync(User, "DocumentReview")).Succeeded
        || await CanManageAsync(pharmacy);

    
    private async Task<bool> CanManageAsync(Pharmacy pharmacy) =>
        (await authorizationService.AuthorizeAsync(User, pharmacy, "PharmacyOwnerOrAdmin")).Succeeded;

    private async Task<(string? Path, IActionResult? Error)> TrySaveAsync(IFormFile file, CancellationToken ct)
    {
        try
        {
            return (await fileStorage.SaveAsync(file, "pharmacy-documents", ct), null);
        }
        catch (InvalidUploadException ex)
        {
            return (null, BadRequest(new ProblemDetails
            {
                Title = "Invalid upload",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest,
            }));
        }
    }

    private async Task<IActionResult?> CheckPharmacyExistsAsync(int pharmacyId, CancellationToken ct)
    {
        var pharmacy = await pharmaciesService.GetByIdAsync(pharmacyId, ct);
        return pharmacy is null ? PharmacyNotFound(pharmacyId) : null;
    }
}