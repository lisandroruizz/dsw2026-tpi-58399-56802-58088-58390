namespace Dsw2026Tpi.CrossCutting.Resources;

public static class ErrorCodeNames
{
    public const string UnhandledError = "UNHANDLED_ERROR";
    public const string ValidationError = "VALIDATION_ERROR";
    public const string AuthenticationFailed = "AUTHENTICATION_FAILED";
    public const string AuthorizationFailed = "AUTHORIZATION_FAILED";
    public const string RegisterUserConflict = "REGISTER_USER_CONFLICT";
    public const string SpecialityNotFound = "SPECIALITY_NOT_FOUND";
    public const string SpecialityNameConflict = "SPECIALITY_NAME_CONFLICT";
    public const string DoctorNotFound = "DOCTOR_NOT_FOUND";
    public const string PatientNotFound = "PATIENT_NOT_FOUND";
    public const string AvailabilityNotFound = "AVAILABILITY_NOT_FOUND";
    public const string AppointmentNotFound = "APPOINTMENT_NOT_FOUND";
    public const string AvailabilityOverlap = "AVAILABILITY_OVERLAP";
    public const string AppointmentConflict = "APPOINTMENT_CONFLICT";
    public const string AppointmentInvalidState = "APPOINTMENT_INVALID_STATE";
    public const string RateLimitExceeded = "RATE_LIMIT_EXCEEDED";
}
