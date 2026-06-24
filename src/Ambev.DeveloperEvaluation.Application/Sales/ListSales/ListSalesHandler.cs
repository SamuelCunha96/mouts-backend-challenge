using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

public class ListSalesHandler : IRequestHandler<ListSalesQuery, ListSalesResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;

    public ListSalesHandler(ISaleRepository saleRepository, IMapper mapper)
    {
        _saleRepository = saleRepository;
        _mapper = mapper;
    }

    public async Task<ListSalesResult> Handle(ListSalesQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _saleRepository.GetAllAsync(
            query.Page,
            query.Size,
            query.Order,
            query.CustomerId,
            query.BranchId,
            query.Status,
            query.MinDate,
            query.MaxDate,
            cancellationToken);

        return new ListSalesResult
        {
            Data = _mapper.Map<IEnumerable<SaleResult>>(items),
            TotalItems = totalCount,
            CurrentPage = query.Page,
            TotalPages = (int)Math.Ceiling(totalCount / (double)query.Size)
        };
    }
}
