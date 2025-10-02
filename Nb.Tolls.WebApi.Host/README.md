# Nb.Tolls Web API

This is the **Nb.Tolls Web API** – a service for calculating toll fees based on vehicle type and timestamps.

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- The project dependencies are managed via NuGet.

### Running the Application

1. Open the solution in Visual Studio or via your preferred IDE.
2. Build the solution to restore NuGet packages.
3. Start the application (F5 or `dotnet run` in the project folder).

Once running, the API is available locally. You can access the Swagger UI to send requests at:


From there, you can test sending toll fee requests by providing a `VehicleType` and a list of timestamps.

---

## How It Works

- The service uses **Nager.Date** NuGet package to handle public holidays.
  > ⚠️ This dependency works for now but could be replaced with a more optimal solution in the future.

- Toll fee data is read from a JSON file located in the `Data` folder (`TollFeesData.json`).
  > ⚠️ Currently, the data source is static. In the future, it should be replaced with a more flexible data source, likely a database.

Swagger (http://localhost:5000/api/norion/index.html) is configured for interactive testing. Example request:

{
"vehicleType": "Car",
"tollTimes": [
"2025-09-30T08:15:00+02:00",
"2025-09-30T09:45:00+02:00",
"2025-09-30T11:14:00+02:00",
"2025-09-30T16:15:00+02:00",
"2025-09-30T17:16:00+02:00",
"2025-09-30T12:15:00+02:00",
"2025-09-29T05:15:00+02:00",
"2025-09-29T08:15:00+02:00",
"2025-09-29T12:15:00+02:00",
"2025-09-29T15:15:00+02:00",
"2025-09-29T16:16:00+02:00",
"2025-09-26T16:16:00+02:00"
]
}

---

## Notes for Future Improvements

- Replace the **Nager.Date** dependency with a better solution or pay.
- Migrate toll fee data from JSON to a database for better maintainability and scalability.
- Add authentication and authorization policies if exposed publicly.

---

## Project Structure

- `Nb.Tolls.WebApi.Host` – API host project.
- `Nb.Tolls.Application` – Application logic (services, helpers, etc.).
- `Nb.Tolls.Infrastructure` – Data access and configuration loaders.
- `Nb.Tolls.Domain` – Domain models
---

## Project Demand Spec:
Varje passage genom en betalstation i Göteborg kostar 8, 13 eller 18 kronor beroende på tidpunkt. Det maximala beloppet per dag och fordon är 60 kronor.

Tider           Belopp
06:00–06:29     8 kr
06:30–06:59     13 kr
07:00–07:59     18 kr
08:00–08:29     13 kr
08:30–14:59     8 kr
15:00–15:29     13 kr
15:30–16:59     18 kr
17:00–17:59     13 kr
18:00–18:29     8 kr
18:30–05:59     0 kr

Trängselskatt tas ut för fordon som passerar en betalstation måndag till fredag mellan 06.00 och 18.29. Skatt tas inte ut lördagar, helgdagar, dagar före helgdag eller under juli månad. Vissa fordon är undantagna från trängselskatt.

En bil som passerar flera betalstationer inom 60 minuter bara beskattas en gång. Det belopp som då ska betalas är det högsta beloppet av de passagerna.

