

using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Application.Services;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Helpers;
using Dsw2026Tpi.CrossCutting.Identity;
using Dsw2026Tpi.Data.Identity;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Dsw2026Tpi.Application.Services; 

public class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISignInService _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly JwtService _jwtService;
    private readonly IPersistence _persistence;
    private readonly ILogger<AuthenticationService> _logger;
    public AuthenticationService(
    UserManager<ApplicationUser> userManager,
    ISignInService signInManager,
    RoleManager<IdentityRole> roleManager,
    JwtService jwtService,
    IPersistence persistence,
    ILogger<AuthenticationService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _jwtService = jwtService;
        _persistence = persistence;
        _logger = logger;
    }
    public async Task<LoginAdminModel.Response> LoginAdmin(LoginAdminModel.Request request)
    {
        ValidateAdminLogin(request);
        string email = request.Email!.Trim().ToLowerInvariant(); 
        ApplicationUser user = await _userManager.FindByEmailAsync(email)
        ?? throw new AuthenticationException();
        if (user.Deleted || !await _signInManager.CheckPassword(user, request.Password!))
        {
            _logger.LogWarning("Intento de login de administrador fallido para {Email}",
            email);
            throw new AuthenticationException();

        }
        IList<string> roles = await _userManager.GetRolesAsync(user);
        if (!roles.Any(role => string.Equals(role, Roles.Administrator,
        StringComparison.OrdinalIgnoreCase)))
        {
            throw new AuthenticationException();
        }
        string token = _jwtService.GenerateToken(
        user.Id,
        user.Email ?? email,
        Roles.Administrator);
        _logger.LogInformation("Administrador autenticado: {Email}", email);
        return new LoginAdminModel.Response(token, Roles.Administrator);
    }


    public async Task<LoginPatientModel.Response> LoginPatient(LoginPatientModel.Request
    request)
    {
        ValidatePatientLogin(request);
        string email = request.Email!.Trim().ToLowerInvariant();
        Patient? patient = await _persistence.First<Patient>(x => x.Dni == request.Dni);
        ApplicationUser user;
        if (patient is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            IdentityResult createUserResult = await _userManager.CreateAsync(user);
            if (!createUserResult.Succeeded)
            {
                throw new ConflictException(
               "REGISTER_USER_CONFLICT",
                "No fue posible registrar automáticamente al paciente.")
                .WithDetail(createUserResult.Errors.Select(error => (error.Code,
                error.Description)));
            }
            await EnsureRoleExists(Roles.Patient);
            IdentityResult roleResult = await _userManager.AddToRoleAsync(user,
            Roles.Patient);
            if (!roleResult.Succeeded)
            {

                await _userManager.DeleteAsync(user);
                throw new ConflictException(
                "REGISTER_USER_CONFLICT",
                "No fue posible asignar el rol PACIENTE.")
                .WithDetail(roleResult.Errors.Select(error => (error.Code,
                error.Description)));
            }
            patient = new Patient(request.Dni, email, user.Id);
            await _persistence.Add(patient);
            try
            {
                await _persistence.SaveChanges();
            }
            catch
            {
                await _userManager.DeleteAsync(user);
                throw;
            }
            _logger.LogInformation("Paciente registrado automáticamente: {Dni}", patient.Dni);
        }
        else
        {
        }
        if (!string.Equals(patient.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            throw new AuthenticationException();
        }
        user = await _userManager.FindByIdAsync(patient.ApplicationUserId)
        ?? throw new AuthenticationException();
        if (user.Deleted)
        {
            throw new AuthenticationException();
        }
        await EnsureRoleExists(Roles.Patient);
        if (!await _userManager.IsInRoleAsync(user, Roles.Patient))
        {
            await _userManager.AddToRoleAsync(user, Roles.Patient);
        }
        string token = _jwtService.GenerateToken(
        user.Id,
        email,
        Roles.Patient,
        patient.Dni);
        _logger.LogInformation("Paciente autenticado: {Dni}", patient.Dni);

        return new LoginPatientModel.Response(token, Roles.Patient);
    }

public async Task<RegisterModel.Response> Register(RegisterModel.Request request)
    {
        ValidateRegister(request);
        string email = request.Email!.Trim().ToLowerInvariant(); 
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        IdentityResult result = await _userManager.CreateAsync(user, request.Password!);
        if (!result.Succeeded)
        {
            throw new ConflictException(
            "REGISTER_USER_CONFLICT",
            "Se produjo un error al registrar el administrador.")
            .WithDetail(result.Errors.Select(error => (error.Code, error.Description)));
        }
        await EnsureRoleExists(Roles.Administrator);
        IdentityResult roleResult = await _userManager.AddToRoleAsync(user,
        Roles.Administrator);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            throw new ConflictException(
            "REGISTER_USER_CONFLICT",
            "No fue posible asignar el rol ADMINISTRADOR.")
            .WithDetail(roleResult.Errors.Select(error => (error.Code,
            error.Description)));
        }
        _logger.LogInformation("Administrador registrado: {Email}", email);
        return new RegisterModel.Response(email);
    }
    private async Task EnsureRoleExists(string role)
    {
        if (!await _roleManager.RoleExistsAsync(role))
        {
            IdentityResult result = await _roleManager.CreateAsync(new IdentityRole(role));
            if (!result.Succeeded)
            {
                throw new ConflictException(
                "REGISTER_USER_CONFLICT",
                $"No fue posible crear el rol {role}.")

                    .WithDetail(result.Errors.Select(error => (error.Code,
error.Description)));
            }
        }
    }
    private static void ValidateAdminLogin(LoginAdminModel.Request request)
    {
        List<(string Field, string Issue)> errors = [];
        if (!request.Email.IsEmailValid())
        {
            errors.Add(("email", "email_ivalido"));
        }
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            errors.Add(("password", "longitud_minima_8"));
        }
        ServiceValidation.ThrowIfAny(errors);
    }
    private static void ValidatePatientLogin(LoginPatientModel.Request request)
    {
        List<(string Field, string Issue)> errors = [];
        if (!request.Email.IsEmailValid())
        {
            errors.Add(("email", "email_invalido"));
        }

        int dniLength = Math.Abs(request.Dni).ToString().Length; 
        if (dniLength < 7 || dniLength > 8)
        {
            errors.Add(("dni", "debe_tener_7_u_8_digitos"));
        }
        ServiceValidation.ThrowIfAny(errors);
    }
    private static void ValidateRegister(RegisterModel.Request request)
    {
        List<(string Field, string Issue)> errors = [];
        if (!request.Email.IsEmailValid())
        {
            errors.Add(("email", "email_invalido"));
        }
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            errors.Add(("password", "longitud_minima_8"));

        }
        ServiceValidation.ThrowIfAny(errors);
    }
}