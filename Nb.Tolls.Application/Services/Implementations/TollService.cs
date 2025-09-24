using Microsoft.Extensions.Logging;
using Nb.Tolls.Domain.Enums;
using Nb.Tolls.Domain.Results;

namespace Nb.Tolls.Application.Services.Implementations;

public class TollService : ITollService
{
    private readonly ILogger<TollService> _logger;

    public TollService(ILogger<TollService> logger)
    {
        _logger = logger;
    }

    public async Task<ApplicationResult<TollResult>> GetTollFeeAsync(
        Vehicle vehicleType,
        DateTimeOffset[] tollRegistrationTime)
    {
        try
        {
            //Todo: implement logic for multiple toll times and max daily fee
             CalculateTollFee(vehicleType, tollRegistrationTime);

            throw new NotImplementedException();
        }
        catch (Exception e)
        {
            _logger.LogError("Exception in GetTollFeeAsync: {Message}", e.Message);
            return ApplicationResult.WithError<TollResult>("Internal error occurred while fetching tolls.");
        }
    }

    internal ApplicationResult<TollResult> CalculateTollFee(Vehicle vehicleType, DateTimeOffset tollRegistrationTime)
    {
        if (IsTollFreeVehicle(vehicleType))
        {
            _logger.LogInformation("Vehicle type {VehicleType} is toll-free.", vehicleType);
            return ApplicationResult.WithSuccess(new TollResult { TollCost = 0 });
        }

        if (IsTollFreeDate(tollRegistrationTime))
        {
            _logger.LogInformation("Date {TollRegistrationTime} is toll-free.", tollRegistrationTime);
            return ApplicationResult.WithSuccess(new TollResult { TollCost = 0 });
        }

        var tollFee = GetTollFee(tollRegistrationTime);
        return ApplicationResult.WithSuccess(new TollResult { TollCost = tollFee });
    }

    internal bool IsTollFreeVehicle(Vehicle vehicle)
    {
        switch (vehicle)
        {
            case Vehicle.Car:
                return false;
            case Vehicle.Motorbike:
            case Vehicle.Tractor:
            case Vehicle.Emergency:
            case Vehicle.Diplomat:
            case Vehicle.Foreign:
            case Vehicle.Military:
                return true;
            default:
                _logger.LogError("Unknown vehicle type: {VehicleType}", vehicle);
                return false;
        }
    }

    internal decimal GetTollFee(DateTimeOffset tollRegistrationTime)
    {
        var hour = tollRegistrationTime.Hour;
        var minute = tollRegistrationTime.Minute;

        //todo: double check these values + move to infrastructure layer with config file
        if (hour == 6 && minute >= 0 && minute <= 29) return 8;
        else if (hour == 6 && minute >= 30 && minute <= 59) return 13;
        else if (hour == 7 && minute >= 0 && minute <= 59) return 18;
        else if (hour == 8 && minute >= 0 && minute <= 29) return 13;
        else if (hour >= 8 && hour <= 14 && minute >= 30 && minute <= 59) return 8;
        else if (hour == 15 && minute >= 0 && minute <= 29) return 13;
        else if (hour == 15 && minute >= 0 || hour == 16 && minute <= 59) return 18;
        else if (hour == 17 && minute >= 0 && minute <= 59) return 13;
        else if (hour == 18 && minute >= 0 && minute <= 29) return 8;
        else return 0;
    }

    internal bool IsTollFreeDate(DateTimeOffset date)
    {
        var year = date.Year;
        var month = date.Month;
        var day = date.Day;

        if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            return true;
        }

        //Todo: double check this. 
        if (year == 2013)
        {
            if (month == 1 && day == 1 ||
                month == 3 && (day == 28 || day == 29) ||
                month == 4 && (day == 1 || day == 30) ||
                month == 5 && (day == 1 || day == 8 || day == 9) ||
                month == 6 && (day == 5 || day == 6 || day == 21) ||
                month == 7 ||
                month == 11 && day == 1 ||
                month == 12 && (day == 24 || day == 25 || day == 26 || day == 31))
            {
                return true;
            }
        }

        return false;
    }
}