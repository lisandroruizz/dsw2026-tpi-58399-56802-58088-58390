using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Enums;

namespace Dsw2026Tpi.Tests;

public class AvailabilitySlotTests
{
    [Fact]
    public void Book_CuandoElSlotEstaDisponible_CambiaElEstadoABooked()
    {
        // Arrange: se preparan la especialidad, el médico,
        // la regla de disponibilidad y el slot.
        var speciality = new Speciality(
            "Cardiología",
            "Diagnóstico y tratamiento de enfermedades del corazón");

        var doctor = new Doctor(
            "Ana Gómez",
            "MP-4521",
            speciality);

        var rule = new AvailabilityRule(
            doctor,
            8,
            2026,
            DayOfWeek.Monday,
            new TimeOnly(9, 0),
            new TimeOnly(12, 0));

        var slot = new AvailabilitySlot(
            rule,
            new DateOnly(2026, 8, 10),
            new TimeOnly(9, 0),
            new TimeOnly(9, 30));

        Assert.Equal(
            AvailabilityStatus.Available,
            slot.Status);

        // Act: se reserva el slot.
        slot.Book();

        // Assert: se comprueba que cambió a reservado.
        Assert.Equal(
            AvailabilityStatus.Booked,
            slot.Status);
    }
}