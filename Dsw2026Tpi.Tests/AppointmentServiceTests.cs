using System;
using System.Collections.Generic;
using System.Text;
using Dsw2026Tpi.Application.Services;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Enums;
using Dsw2026Tpi.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Dsw2026Tpi.Tests; 

public class AppointmentServiceTests
{
    private readonly IPersistence _mockPersistence = Substitute.For<IPersistence>();
    private readonly ILogger<AppointmentService> _mockLogger = Substitute.For<ILogger<AppointmentService>>();


    private const long PatientDni = 30111222; 

    //Crea una cita reservada junto con su disponibilidad 
    private static (Appointment Appointment, AvailabilitySlot Slot, Patient Patient) CrearCitaReservada()
    {
        var speciality = new Speciality("Cardiología", "Diagnóstico y tratamiento de enfermedades del corazón");
        var doctor = new Doctor("Ana Gómez", "MP-4521", speciality);
        var regla = new AvailabilityRule(doctor, 8, 2026, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(12, 0));
        var slot = new AvailabilitySlot(regla, new DateOnly(2026, 8, 10), new TimeOnly(9, 0), new TimeOnly(9, 30));
        slot.Book();

        var patient = new Patient(PatientDni, "paciente@test.com", "user-001");
        var appointment = new Appointment(slot, patient, "Control de rutina");

        return (appointment, slot, patient); 

    }

    [Fact]
    public async Task Cancel_CuandoLaCitaNoExiste_EntoncesSeLanzaEntityNotFoundException()
    {
        //Arrange - Inicio 
        Appointment? cita = null;
        _mockPersistence.GetById<Appointment>(Arg.Any<Guid>(), Arg.Any<string[]>()).Returns(cita);

        var service = new AppointmentService(_mockPersistence, _mockLogger);

        //Act - Ejecucuión 
        var ex = await Assert.ThrowsAsync<EntityNotFoundException>(() => service.Cancel(Guid.NewGuid(), PatientDni));


        //Assert - Verificación
        Assert.IsType<EntityNotFoundException>(ex); 
    }

}
