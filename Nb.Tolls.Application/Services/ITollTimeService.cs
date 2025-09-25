namespace Nb.Tolls.Application.Services;

public interface ITollTimeService
{
    Task<List<DateTimeOffset>> ExtractEligibleTollFeeTimes(List<DateTimeOffset> tollTimes);
    IReadOnlyList<DateTimeOffset> ExtractNonOverlappingTollTimes(List<DateTimeOffset> tollTimes);
    IReadOnlyList<DateTimeOffset> ExtractOverlappingTollTimes(List<DateTimeOffset> tollTimes);
}