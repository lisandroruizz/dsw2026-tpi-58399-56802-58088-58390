using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Domain.Entities;

public class Patient : EntityBase
{
    public long Dni { get; private set; }
    public string Email { get; private set; }
    public string ApplicationUserId { get; private set; }
    public string? FullName { get; private set; }
    public string? Phone { get; private set; }

    public ICollection<Appointment> Appointments { get; private set; } = new List<Appointment>();


    #region Constructor for EF 
#pragma warning disable CS8648
    private Patient() { }
#pragma warning restore CS8618
    #endregion

    public Patient(long dni, string email, string applicationUserId, Guid? id = null) : base(id)
    {
        Dni = dni;
        Email = email.Trim().ToLowerInvariant();
        ApplicationUserId = applicationUserId; 
    }

    public void UpdateContactData(string? fullName, string? phone)
    {
        FullName = string.IsNullOrWhiteSpace(fullName) ? null : fullName.Trim();
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(); 

    }



}
