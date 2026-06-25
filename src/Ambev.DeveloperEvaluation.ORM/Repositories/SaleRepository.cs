using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.ORM.Repositories;

public class SaleRepository : ISaleRepository
{
    private readonly DefaultContext _context;

    public SaleRepository(DefaultContext context)
    {
        _context = context;
    }

    public async Task<Sale> CreateAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        await _context.Sales.AddAsync(sale, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return sale;
    }

    public async Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Sales
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<Sale?> GetBySaleNumberAsync(string saleNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Sales
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.SaleNumber == saleNumber, cancellationToken);
    }

    public async Task<Sale> UpdateAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        _context.Sales.Update(sale);
        await _context.SaveChangesAsync(cancellationToken);
        return sale;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sale = await GetByIdAsync(id, cancellationToken);
        if (sale == null)
            return false;

        _context.Sales.Remove(sale);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<(IEnumerable<Sale> Items, int TotalCount)> GetAllAsync(
        int page,
        int size,
        string? order = null,
        Guid? customerId = null,
        Guid? branchId = null,
        string? status = null,
        DateTime? minDate = null,
        DateTime? maxDate = null,
        string? customerName = null,
        string? branchName = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Sales.Include(s => s.Items).AsQueryable();

        if (customerId.HasValue)
            query = query.Where(s => s.CustomerId == customerId.Value);

        if (branchId.HasValue)
            query = query.Where(s => s.BranchId == branchId.Value);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<SaleStatus>(status, true, out var parsedStatus))
            query = query.Where(s => s.Status == parsedStatus);

        if (minDate.HasValue)
            query = query.Where(s => s.SaleDate >= minDate.Value);

        if (maxDate.HasValue)
            query = query.Where(s => s.SaleDate <= maxDate.Value);

        if (!string.IsNullOrWhiteSpace(customerName))
        {
            var pattern = ToLikePattern(customerName);
            query = query.Where(s => EF.Functions.ILike(s.CustomerName, pattern));
        }

        if (!string.IsNullOrWhiteSpace(branchName))
        {
            var pattern = ToLikePattern(branchName);
            query = query.Where(s => EF.Functions.ILike(s.BranchName, pattern));
        }

        query = ApplyOrdering(query, order);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    private static string ToLikePattern(string value)
    {
        var pattern = value;
        if (pattern.StartsWith('*')) pattern = "%" + pattern[1..];
        if (pattern.EndsWith('*')) pattern = pattern[..^1] + "%";
        return pattern;
    }

    private static IQueryable<Sale> ApplyOrdering(IQueryable<Sale> query, string? order)
    {
        if (string.IsNullOrWhiteSpace(order))
            return query.OrderByDescending(s => s.SaleDate);

        var parts = order.Split(',', StringSplitOptions.RemoveEmptyEntries);
        IOrderedQueryable<Sale>? ordered = null;

        foreach (var part in parts)
        {
            var tokens = part.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var field = tokens[0].ToLower();
            var descending = tokens.Length > 1 && tokens[1].Equals("desc", StringComparison.OrdinalIgnoreCase);

            ordered = (field, descending, ordered == null) switch
            {
                ("salenumber",  false, true)  => query.OrderBy(s => s.SaleNumber),
                ("salenumber",  true,  true)  => query.OrderByDescending(s => s.SaleNumber),
                ("salenumber",  false, false) => ordered!.ThenBy(s => s.SaleNumber),
                ("salenumber",  true,  false) => ordered!.ThenByDescending(s => s.SaleNumber),
                ("saledate",    false, true)  => query.OrderBy(s => s.SaleDate),
                ("saledate",    true,  true)  => query.OrderByDescending(s => s.SaleDate),
                ("saledate",    false, false) => ordered!.ThenBy(s => s.SaleDate),
                ("saledate",    true,  false) => ordered!.ThenByDescending(s => s.SaleDate),
                ("customername",false, true)  => query.OrderBy(s => s.CustomerName),
                ("customername",true,  true)  => query.OrderByDescending(s => s.CustomerName),
                ("customername",false, false) => ordered!.ThenBy(s => s.CustomerName),
                ("customername",true,  false) => ordered!.ThenByDescending(s => s.CustomerName),
                ("branchname",  false, true)  => query.OrderBy(s => s.BranchName),
                ("branchname",  true,  true)  => query.OrderByDescending(s => s.BranchName),
                ("branchname",  false, false) => ordered!.ThenBy(s => s.BranchName),
                ("branchname",  true,  false) => ordered!.ThenByDescending(s => s.BranchName),
                ("totalamount", false, true)  => query.OrderBy(s => s.TotalAmount),
                ("totalamount", true,  true)  => query.OrderByDescending(s => s.TotalAmount),
                ("totalamount", false, false) => ordered!.ThenBy(s => s.TotalAmount),
                ("totalamount", true,  false) => ordered!.ThenByDescending(s => s.TotalAmount),
                _ => ordered ?? query.OrderByDescending(s => s.SaleDate)
            };
        }

        return ordered ?? query.OrderByDescending(s => s.SaleDate);
    }
}
