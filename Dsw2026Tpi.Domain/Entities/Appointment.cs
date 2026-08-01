using System;
using System.Collections.Generic;
using System.Text;
using Dsw2026Tpi.Domain.Enums;

namespace Dsw2026Tpi.Domain.Entities;

public class Appointment : EntityBase
{
    public Guid AvailabilityId { get; private set; }
    public Availability Availability { get; private set; }
    public Guid PatientId { get; private set; }
    public Patient Patient { get; private set; }
    public string Reason { get; private set; }
    public AppointmentStatus Status { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    #region Constructor for EF 

#pragma warning disable CS8618
    private Appointment() { }
#pragma warning restore CS8618
    #endregion
    public Appointment(Availability availability, Patient patient, string reason, Guid? id = null) : base(id)
    {
        Availability = availability;
        AvailabilityId = availability.Id;
        Patient = patient;
        PatientId = patient.Id;
        Reason = reason.Trim();
        Status = AppointmentStatus.Booked;
    }
    public void Cancel()
    {

        if (Status != AppointmentStatus.Booked)
        {
            throw new InvalidOperationException("Solo se pueden cancelar citas reservadas.");
        }
        Status = AppointmentStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
    }
    public void MarkAsAttended()
    {
        if (Status != AppointmentStatus.Booked)
        {
            throw new InvalidOperationException("Solo una cita reservada puede marcarse como atendida.");
        }

        Status = AppointmentStatus.Attended;
    }

    public void MarkAsNoShow()
    {
        if (Status != AppointmentStatus.Booked)
        {
            throw new InvalidOperationException("Solo una cita reservada puede marcarse como ausente.");
        }
        Status = AppointmentStatus.NoShow;
    }
}





