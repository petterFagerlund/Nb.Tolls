namespace Nb.Tolls.Application.Services;

public interface ITollDateService
{
    Task<bool> IsTollFreeDate(DateTime dateTime);
}