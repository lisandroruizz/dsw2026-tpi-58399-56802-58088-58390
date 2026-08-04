using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Helpers;
using Dsw2026Tpi.CrossCutting.Resources;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Application.Services;

internal static class ServiceValidation
{
    public static void ValidatePagination(int pageSize, int pageIndex)
    {
        List<(string Field, string Issue)> errors = [];

        if (pageSize <= 0)
        {
            errors.Add(("pageSize", "debe_ser_mayor_que_cero"));
        }

        if (pageIndex < 0)
        {
            errors.Add(("pageIndex", "debe_ser_cero_o_mayor"));
        }

        ThrowIfAny(errors);
    }

    public static void ValidateOptionalName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return;

        var length = name.Trim().Length;

        if (length < 3 || length > 100)
        {
            throw new ValidationException(
                ErrorCodes.VALIDATION_ERROR,
                nameof(ErrorCodes.VALIDATION_ERROR));
        }
    }

    public static void ThrowIfAny(
      IEnumerable<(string Field, string Issue)> errors)
    {
        var errorList = errors.ToArray();

        if (errorList.Length == 0)
            return;

        throw new ValidationException(errorList);
    }
}
