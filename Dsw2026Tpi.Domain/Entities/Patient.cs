using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Domain.Entities;

public class Patient : EntityBase
{
    public long Dni { get; private set; }
    public string Email { get; private set; }
    public string ApplicationUserId { get; private set; }
    public string? Name { get; private set; }
    public string? Phone { get; private set; }

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

    public void UpdateContactData(string? name, string? phone)
    {
        Name = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(); 
    }



}
