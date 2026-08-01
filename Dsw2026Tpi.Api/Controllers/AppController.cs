using Microsoft.AspNetCore.Mvc;
using Dsw2026Tpi.CrossCutting.Exceptions;
using System.Security.Claims; 


namespace Dsw2026Tpi.Api.Controllers;

/// <summary>
/// Clase base para configuraciones generales de controladores
/// </summary>
[ApiController]

public abstract class AppController : ControllerBase
{
    protected long GetAuthenticatedPatientDni()
    {
        string? dniClaim = User.FindFirstValue("dni"); 
        if(!long.TryParse(dniClaim, out long dni))
        {
            throw new AuthorizationException(); 
        }

        return dni; 
    }

}

