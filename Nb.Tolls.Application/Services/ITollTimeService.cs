namespace Nb.Tolls.Application.Services;

public interface ITollTimeService
{
    Task<List<DateTime>> GetEligibleTollFeeTimes(List<DateTime> tollTimes);
    List<DateTime> GetNonOverlappingTollTimes(List<DateTime> tollTimes);
    List<DateTime> GetOverlappingTollTimes(List<DateTime> tollTimes);
}