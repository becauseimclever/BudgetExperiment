// <copyright file="LocationReportBuilder.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Reports;

/// <summary>
/// Builds location-based spending reports by grouping expense transactions
/// by geographic region and city.
/// </summary>
public sealed class LocationReportBuilder : ILocationReportBuilder
{
    private readonly ITransactionQueryRepository _transactionRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocationReportBuilder"/> class.
    /// </summary>
    /// <param name="transactionRepository">The transaction repository.</param>
    public LocationReportBuilder(ITransactionQueryRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    /// <inheritdoc/>
    public async Task<LocationSpendingReportDto> GetSpendingByLocationAsync(
        DateOnly startDate,
        DateOnly endDate,
        Guid? accountId = null,
        CancellationToken cancellationToken = default)
    {
        var transactions = await _transactionRepository.GetByDateRangeAsync(
            startDate, endDate, accountId, cancellationToken);

        var nonTransferTransactions = transactions.Where(t => !t.IsTransfer).ToList();

        var expenseTransactions = nonTransferTransactions
            .Where(t => t.Amount.Amount < 0)
            .ToList();

        var totalSpending = expenseTransactions.Sum(t => Math.Abs(t.Amount.Amount));

        var withLocation = expenseTransactions
            .Where(t => t.Location is not null && t.Location.StateOrRegion is not null)
            .ToList();

        var regions = BuildRegionSpending(withLocation, totalSpending);

        return new LocationSpendingReportDto
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalSpending = totalSpending,
            TotalTransactions = nonTransferTransactions.Count,
            TransactionsWithLocation = withLocation.Count,
            Regions = regions,
        };
    }

    private static List<RegionSpendingDto> BuildRegionSpending(
        List<Transaction> withLocation,
        decimal totalSpending)
    {
        var regionGroups = withLocation
            .GroupBy(t => new { t.Location!.Country, t.Location.StateOrRegion })
            .ToList();

        var regions = new List<RegionSpendingDto>();

        foreach (var group in regionGroups)
        {
            var regionSpending = group.Sum(t => Math.Abs(t.Amount.Amount));
            var percentage = totalSpending > 0
                ? Math.Round(regionSpending / totalSpending * 100, 2)
                : 0m;

            var country = group.Key.Country ?? string.Empty;
            var state = group.Key.StateOrRegion!;
            var regionCode = string.IsNullOrEmpty(country) ? state : $"{country}-{state}";

            var cityGroups = BuildCitySpending(group, regionSpending);

            regions.Add(new RegionSpendingDto
            {
                RegionCode = regionCode,
                RegionName = state,
                Country = country,
                TotalSpending = regionSpending,
                TransactionCount = group.Count(),
                Percentage = percentage,
                Cities = cityGroups.Count > 0 ? cityGroups : null,
            });
        }

        return regions.OrderByDescending(r => r.TotalSpending).ToList();
    }

    private static List<CitySpendingDto> BuildCitySpending(
        IGrouping<dynamic, Transaction> group,
        decimal regionSpending)
    {
        return group
            .Where(t => t.Location!.City is not null)
            .GroupBy(t => t.Location!.City!)
            .Select(cg => new CitySpendingDto
            {
                City = cg.Key,
                TotalSpending = cg.Sum(t => Math.Abs(t.Amount.Amount)),
                TransactionCount = cg.Count(),
                Percentage = regionSpending > 0
                    ? Math.Round(cg.Sum(t => Math.Abs(t.Amount.Amount)) / regionSpending * 100, 2)
                    : 0m,
            })
            .OrderByDescending(c => c.TotalSpending)
            .ToList();
    }
}
