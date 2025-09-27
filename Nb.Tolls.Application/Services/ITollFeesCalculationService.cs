using Nb.Tolls.Domain.Results;

namespace Nb.Tolls.Application.Services;

public interface ITollFeesCalculationService
{
    ApplicationResult<TollFeesResult> CalculateNonOverlappingTollFees(IReadOnlyList<DateTime> nonOverlappingTollTimes);
    ApplicationResult<TollFeesResult> CalculateOverlappingTollFees(IReadOnlyList<DateTime> overlappingTollTimes);
}