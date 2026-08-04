namespace Dsw2026Tpi.Domain.Interfaces;

public interface INonWorkingDayProvider
{
    bool IsNonWorkingDay(DateOnly date);
}
