using Dsw2026Tpi.Domain.Interfaces;
using System.Globalization;
using System.Text.Json;

namespace Dsw2026Tpi.Api.Providers;

public sealed class JsonNonWorkingDayProvider
    : INonWorkingDayProvider
{
    private readonly IReadOnlySet<DateOnly>
        _nonWorkingDays;

    public JsonNonWorkingDayProvider(
        IWebHostEnvironment environment)
    {
        string path = Path.Combine(
            environment.ContentRootPath,
            "Sources",
            "non-working-days.json");

        if (!File.Exists(path))
        {
            throw new InvalidOperationException(
                $"No se encontró el archivo de días no laborables: {path}");
        }

        string json =
            File.ReadAllText(path);

        string[] values =
            JsonSerializer
                .Deserialize<string[]>(json)
            ?? [];

        var parsedDates =
            new HashSet<DateOnly>();

        foreach (string value in values)
        {
            if (!DateOnly.TryParseExact(
                value,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateOnly date))
            {
                throw new InvalidOperationException(
                    $"La fecha '{value}' del archivo de días no laborables no tiene el formato yyyy-MM-dd.");
            }

            parsedDates.Add(date);
        }

        _nonWorkingDays = parsedDates;
    }

    public bool IsNonWorkingDay(
        DateOnly date)
    {
        return _nonWorkingDays.Contains(date);
    }
}
