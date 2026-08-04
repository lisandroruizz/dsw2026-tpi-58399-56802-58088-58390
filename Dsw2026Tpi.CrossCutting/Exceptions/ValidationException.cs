using Dsw2026Tpi.CrossCutting.Resources;

namespace Dsw2026Tpi.CrossCutting.Exceptions;

/// <summary>
/// Excepción que se lanza cuando los datos de entrada no son válidos.
/// Puede incluir múltiples errores de validación.
/// </summary>
public class ValidationException : AppException
{
    public ValidationException()
        : base(
             ErrorCodeNames.ValidationError,
            ErrorCodes.VALIDATION_ERROR)
    {
    }

    public ValidationException(
        IEnumerable<(
            string Field,
            string Issue)> details)
        : this()
    {
        WithDetail(details);
    }

    public ValidationException(string message, string errorCode)
        : base(message, errorCode)
    {
    }
}
