namespace Nb.Tolls.Infrastructure.Models;

public class TollFeesModel
{
    public required string Start { get; init; }
    public required string End { get; init; }
    public required int AmountSek { get; init; }
    public required int StartMin { get; init; }
    public required int EndMin { get; init; }
}