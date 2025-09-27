namespace Nb.Tolls.Application.Services;

public interface ITollDateService
{
    Task<bool> IsTollFreeDateAsync(DateTime dateTime);
}