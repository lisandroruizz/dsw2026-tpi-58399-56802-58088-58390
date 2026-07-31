using Dsw2026Tpi.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Domain.Entities
{
    public class Availability : EntityBase
    {
        public Guid DoctorId { get; private set; }
        public Doctor Doctor { get; private set; }
        public DateOnly Date { get; private set; }
        public TimeOnly StartTime { get; private set; }
        public TimeOnly EndTime { get; private set; }
        public AvailabilityStatus Status { get; private set; } 

        #region Constructor for EF
#pragma warning disable CS8618
        private Availability() { }
#pragma warning restore CS8618
        #endregion

        public Availability(Doctor doctor, DateOnly date, TimeOnly startTime, TimeOnly endTime, Guid? id = null)
            : base(id)
        {
            Doctor = doctor;
            DoctorId = doctor.Id;
            Date = date;
            StartTime = startTime;
            EndTime = endTime;
            Status = AvailabilityStatus.Available;
        }

        public DateTime GetStartDateTime()
        {
            return Date.ToDateTime(StartTime);
        }

        public void Reserve()
        {
            if (Status != AvailabilityStatus.Available)
            {
                throw new InvalidOperationException("La disponibilidad no se encuentra libre.");
            }

            Status = AvailabilityStatus.Reserved;
        }

        public void Release()
        {
            if (Status != AvailabilityStatus.Reserved)
            {
                throw new InvalidOperationException("Solo se puede liberar una disponibilidad reservada.");
            }

            Status = AvailabilityStatus.Available;
        }

        public void Block()
        {
            if (Status == AvailabilityStatus.Reserved)
            {
                throw new InvalidOperationException("No se puede bloquear una disponibilidad reservada.");
            }

            Status = AvailabilityStatus.Booked;
        }
    }
}
