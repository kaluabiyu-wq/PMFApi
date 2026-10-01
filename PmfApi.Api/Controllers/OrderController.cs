using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PmfApi.Application.Dtos;
using PmfApi.Application.Interfaces;
using PmfApi.Domain.Entities;

namespace PmfApi.Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
[Tags("Orders")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class OrderController(
    IOrderService orderService,
    IOrderItemService orderItemService,
    IPharmaciesService pharmaciesService,
    IAuthorizationService authorizationService) : ControllerBase
{
    

    [HttpPost("orders", Name = nameof(CreateOrder))]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Place an order")]
    [EndpointDescription("Creates a Pending order for the signed-in patient. Send only the items (inventoryId + quantity); the patient is taken from the token and the pharmacy is derived from the items, which must all belong to the same active pharmacy. Each line's unit price is copied from the inventory row at this moment.")]
    public async Task<IActionResult> CreateOrder(OrderRequest request, CancellationToken ct)
    {
        if (CurrentUserId is not { } userId) return Unauthorized();

        var result = await orderService.CreateAsync(userId, request, ct);
        return ToResult(result, order => CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, order));
    }

    [HttpGet("orders", Name = nameof(GetMyOrders))]
    [ProducesResponseType(typeof(PagedResponse<OrderResponse>), StatusCodes.Status200OK)]
    [EndpointSummary("List my orders")]
    [EndpointDescription("Returns the signed-in patient's own orders, newest first, optionally filtered by status (Pending, Confirmed, Cancelled).")]
    public async Task<IActionResult> GetMyOrders([FromQuery] OrderListQuery query, CancellationToken ct)
    {
        if (CurrentUserId is not { } userId) return Unauthorized();

        return Ok(await orderService.GetByUserAsync(userId, query, ct));
    }

    [HttpGet("orders/{id:int}", Name = nameof(GetOrderById))]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get one order")]
    [EndpointDescription("Visible to the patient who placed it, to active staff of the pharmacy it was placed with, and to a SystemAdmin. For anyone else the answer is 404, not 403, so order ids cannot be probed.")]
    public async Task<IActionResult> GetOrderById(int id, CancellationToken ct)
    {
        var access = await GetAccessAsync(id, ct);
        return access.Order is null ? OrderNotFound(id) : Ok(access.Order);
    }

    [HttpPost("orders/{orderId:int}/items", Name = nameof(AddOrderItem))]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Add an item to a pending order")]
    [EndpointDescription("Patient only, and only while the order is Pending. The item must belong to the same pharmacy as the rest of the order. Returns the whole updated order, because the total changes.")]
    public async Task<IActionResult> AddOrderItem(int orderId, OrderItemRequest request, CancellationToken ct)
    {
        var denied = await RequirePatientAsync(orderId, ct);
        if (denied is not null) return denied;

        var result = await orderItemService.AddAsync(orderId, request, ct);
        return ToResult(result, order => CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, order));
    }

    [HttpPut("orders/{orderId:int}/items/{itemId:int}", Name = nameof(UpdateOrderItem))]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Change an item's quantity")]
    [EndpointDescription("Patient only, and only while the order is Pending. The line keeps the unit price it was added with.")]
    public async Task<IActionResult> UpdateOrderItem(int orderId, int itemId, OrderItemQuantityRequest request, CancellationToken ct)
    {
        var denied = await RequirePatientAsync(orderId, ct);
        if (denied is not null) return denied;

        var result = await orderItemService.UpdateQuantityAsync(orderId, itemId, request.Quantity, ct);
        return ToResult(result, order => Ok(order));
    }

    [HttpDelete("orders/{orderId:int}/items/{itemId:int}", Name = nameof(RemoveOrderItem))]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Remove an item from a pending order")]
    [EndpointDescription("Patient only, and only while the order is Pending. The last remaining item cannot be removed; cancel the order instead.")]
    public async Task<IActionResult> RemoveOrderItem(int orderId, int itemId, CancellationToken ct)
    {
        var denied = await RequirePatientAsync(orderId, ct);
        if (denied is not null) return denied;

        var result = await orderItemService.RemoveAsync(orderId, itemId, ct);
        return ToResult(result, order => Ok(order));
    }


    [HttpGet("pharmacies/{pharmacyId:int}/orders", Name = nameof(GetPharmacyOrders))]
    [ProducesResponseType(typeof(PagedResponse<OrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("List orders placed with a pharmacy")]
    [EndpointDescription("The pharmacy's inbox, newest first, optionally filtered by status. Requires the PharmacyOwnerOrAdmin policy evaluated against this pharmacy: active staff of THIS pharmacy holding the PharmacyStaff or PharmacyAdmin role, or a SystemAdmin.")]
    public async Task<IActionResult> GetPharmacyOrders(int pharmacyId, [FromQuery]
      OrderListQuery query, CancellationToken ct)
    {
        var pharmacy = await pharmaciesService.GetEntityByIdAsync(pharmacyId, ct);
        if (pharmacy is null)
            return NotFound(new ProblemDetails
            {
                Title = "Pharmacy not found",
                Detail = $"No pharmacy exists with id {pharmacyId}.",
                Status = StatusCodes.Status404NotFound,
            });

        var auth = await authorizationService.AuthorizeAsync(User, pharmacy, "PharmacyOwnerOrAdmin");
        if (!auth.Succeeded) return Forbid();

        return Ok(await orderService.GetByPharmacyAsync(pharmacyId, query, ct));
    }

    [HttpPost("orders/{id:int}/confirm", Name = nameof(ConfirmOrder))]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Confirm a pending order")]
    [EndpointDescription("Pharmacy staff only (PharmacyOwnerOrAdmin against the order's pharmacy). Pending -> Confirmed. Returns 409 if the order is no longer Pending, including when it was cancelled a moment earlier.")]
    public async Task<IActionResult> ConfirmOrder(int id, CancellationToken ct)
    {
        var access = await GetAccessAsync(id, ct);
        if (access.Order is null) return OrderNotFound(id);
        if (!access.IsPharmacy) return Forbid();

        var result = await orderService.ChangeStatusAsync(id, OrderStatus.Confirmed,
         byPharmacy: true, ct);
        return ToResult(result, order => Ok(order));
    }

    [HttpPost("orders/{id:int}/cancel", Name = nameof(CancelOrder))]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Cancel an order")]
    [EndpointDescription("The patient can cancel while the order is Pending. The pharmacy can cancel while it is Pending or Confirmed. Cancelled is final.")]
    public async Task<IActionResult> CancelOrder(int id, CancellationToken ct)
    {
        var access = await GetAccessAsync(id, ct);
        if (access.Order is null) return OrderNotFound(id);

        var result = await orderService.ChangeStatusAsync(id, OrderStatus.Cancelled,
         byPharmacy: access.IsPharmacy, ct);
        return ToResult(result, order => Ok(order));
    }

    

    private int? CurrentUserId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private sealed record OrderAccess(OrderResponse? Order, bool IsPatient, bool IsPharmacy);

    
    private async Task<OrderAccess> GetAccessAsync(int orderId, CancellationToken ct)
    {
        var order = await orderService.GetByIdAsync(orderId, ct);
        if (order is null) return new OrderAccess(null, false, false);

        var isPatient = CurrentUserId == order.UserId;

        var isPharmacy = false;
        var pharmacy = await pharmaciesService.GetEntityByIdAsync(order.PharmacyId, ct);
        if (pharmacy is not null)
            isPharmacy = (await authorizationService.AuthorizeAsync(User, pharmacy,
             "PharmacyOwnerOrAdmin")).Succeeded;

        return isPatient || isPharmacy
            ? new OrderAccess(order, isPatient, isPharmacy)
            : new OrderAccess(null, false, false);
    }

    private async Task<IActionResult?> RequirePatientAsync(int orderId, CancellationToken ct)
    {
        var access = await GetAccessAsync(orderId, ct);
        if (access.Order is null) return OrderNotFound(orderId);
        if (!access.IsPatient) return Forbid();
        return null;
    }

    private IActionResult OrderNotFound(int id) => NotFound(new ProblemDetails
    {
        Title = "Order not found",
        Detail = $"No order exists with id {id}.",
        Status = StatusCodes.Status404NotFound,
    });

    private IActionResult ToResult<T>(ServiceResult<T> result,
     Func<T, IActionResult> onSuccess)
      => result.OutCome switch
    {
        ServiceOutCome.Success => onSuccess(result.Value!),
        ServiceOutCome.NotFound => NotFound(
            new ProblemDetails {
                 Title = "Not found",
                 Detail = result.Error,
                 Status = StatusCodes.Status404NotFound 
                 }),
        ServiceOutCome.Conflict => Conflict(
            new ProblemDetails {
                 Title = "Conflict", 
                 Detail = result.Error, 
                 Status = StatusCodes.Status409Conflict }),
        _ => BadRequest(new ProblemDetails { 
                 Title = "Invalid request", 
                 Detail = result.Error, 
                 Status = StatusCodes.Status400BadRequest }),
    };
}