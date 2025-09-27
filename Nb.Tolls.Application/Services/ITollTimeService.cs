namespace Nb.Tolls.Application.Services;

public interface ITollTimeService
{
    Task<List<DateTime>> GetEligibleTollFeeTimes(List<DateTime> tollTimes);
    IReadOnlyList<DateTime> GetNonOverlappingTollTimes(List<DateTime> tollTimes);
    IReadOnlyList<DateTime> GetOverlappingTollTimes(List<DateTime> tollTimes);
}