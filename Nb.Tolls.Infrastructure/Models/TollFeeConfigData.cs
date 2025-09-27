namespace Nb.Tolls.Infrastructure.Models;

internal class TollFeeConfigData
{
    public required string Timezone { get; init; }
    public required string Semantics { get; init; }
    public required List<TollFeesModel> TollFees { get; init; }
}