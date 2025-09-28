using Nb.Tolls.Domain.Results;

namespace Nb.Tolls.Application.Services;

public interface ITollFeesCalculationService
{
    ApplicationResult<DailyTollFeesResult> CalculateNonOverlappingTollFees(IReadOnlyList<DateTime> nonOverlappingTollTimes);
    ApplicationResult<DailyTollFeesResult> CalculateOverlappingTollFees(IReadOnlyList<DateTime> overlappingTollTimes);
}