using Ambev.DeveloperEvaluation.Application.Sales.CancelSale;
using Ambev.DeveloperEvaluation.Application.Sales.CancelSaleItem;
using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Application.Sales.GetSale;
using Ambev.DeveloperEvaluation.Application.Sales.ListSales;
using Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;
using Ambev.DeveloperEvaluation.WebApi.Common;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.UpdateSale;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales;

[ApiController]
[Route("api/[controller]")]
public class SalesController : BaseController
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public SalesController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator;
        _mapper = mapper;
    }

    /// <summary>Creates a new sale.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponseWithData<SaleResult>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSale([FromBody] CreateSaleRequest request, CancellationToken cancellationToken)
    {
        var validator = new CreateSaleRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        var command = _mapper.Map<CreateSaleCommand>(request);
        var result = await _mediator.Send(command, cancellationToken);

        return Created(string.Empty, new ApiResponseWithData<SaleResult>
        {
            Success = true,
            Message = "Sale created successfully",
            Data = result
        });
    }

    /// <summary>Returns a paginated list of sales with optional filters.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponseWithData<ListSalesResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListSales(
        [FromQuery] int _page = 1,
        [FromQuery] int _size = 10,
        [FromQuery] string? _order = null,
        [FromQuery] Guid? customerId = null,
        [FromQuery] Guid? branchId = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? minDate = null,
        [FromQuery] DateTime? maxDate = null,
        [FromQuery] string? customerName = null,
        [FromQuery] string? branchName = null,
        CancellationToken cancellationToken = default)
    {
        var query = new ListSalesQuery
        {
            Page = _page,
            Size = _size,
            Order = _order,
            CustomerId = customerId,
            BranchId = branchId,
            Status = status,
            MinDate = minDate,
            MaxDate = maxDate,
            CustomerName = customerName,
            BranchName = branchName
        };

        var result = await _mediator.Send(query, cancellationToken);

        return Ok(new ApiResponseWithData<ListSalesResult>
        {
            Success = true,
            Message = "Sales retrieved successfully",
            Data = result
        });
    }

    /// <summary>Returns a single sale by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponseWithData<SaleResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSale([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSaleQuery(id), cancellationToken);

        return Ok(new ApiResponseWithData<SaleResult>
        {
            Success = true,
            Message = "Sale retrieved successfully",
            Data = result
        });
    }

    /// <summary>Updates an existing sale.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponseWithData<SaleResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSale([FromRoute] Guid id, [FromBody] UpdateSaleRequest request, CancellationToken cancellationToken)
    {
        var validator = new UpdateSaleRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        var command = _mapper.Map<UpdateSaleCommand>(request);
        command.Id = id;

        var result = await _mediator.Send(command, cancellationToken);

        return Ok(new ApiResponseWithData<SaleResult>
        {
            Success = true,
            Message = "Sale updated successfully",
            Data = result
        });
    }

    /// <summary>Cancels a sale.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelSale([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new CancelSaleCommand(id), cancellationToken);

        return Ok(new ApiResponse
        {
            Success = true,
            Message = "Sale cancelled successfully"
        });
    }

    /// <summary>Cancels a specific item within a sale.</summary>
    [HttpPatch("{id:guid}/items/{productId:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelSaleItem([FromRoute] Guid id, [FromRoute] Guid productId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CancelSaleItemCommand(id, productId), cancellationToken);

        return Ok(new ApiResponse
        {
            Success = true,
            Message = result.Message
        });
    }
}
