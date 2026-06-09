using Logbook.Api.Models;

namespace Logbook.Api.Validators;

public static class QsoDetailsValidator
{
    /// <summary>
    /// Validates and throws ArgumentException if validation fails
    /// </summary>
    /// <param name="qso">The QsoDetails to validate</param>
    /// <exception cref="ArgumentException">Thrown when validation fails</exception>
    public static void ValidateAndThrow(QsoDetails qso)
    {
        var errors = Validate(qso);

        if (errors.Count > 0)
        {
            throw new ArgumentException(string.Join("; ", errors), nameof(qso));
        }
    }

    /// <summary>
    /// Validates QsoDetails for update operations
    /// </summary>
    /// <param name="qso">The QsoDetails to validate</param>
    /// <returns>List of validation error messages. Empty if valid.</returns>
    private static List<string> Validate(QsoDetails qso)
    {
        var errors = new List<string>();

        ValidationRules.ValidateCallSign(errors, qso.Call);
        ValidationRules.ValidateBand(errors, qso.Band);
        ValidationRules.ValidateQsoMode(errors, qso.Mode);

        if (qso.Date == default)
            errors.Add("Date is required");
        else if (qso.Date > DateTime.UtcNow.AddDays(1))
            errors.Add("Date cannot be in the future");

        // Optional field validations
        if (qso.Freq is < 0)
            errors.Add("Frequency cannot be negative");

        if (qso.FreqRx is < 0)
            errors.Add("Receive frequency cannot be negative");

        ValidationRules.ValidateRst(errors, qso.RstSent, false);
        ValidationRules.ValidateRst(errors, qso.RstRcvd, false);

        ValidationRules.ValidateGrid(errors, qso.Grid, false);

        if (qso.MyCqZone is < 1 or > 40)
            errors.Add("My CQ Zone must be between 1 and 40");

        if (qso.MyItuZone is < 1 or > 90)
            errors.Add("My ITU Zone must be between 1 and 90");

        ValidationRules.ValidateCallSign(errors, qso.StationCallsign, false);
        ValidationRules.ValidateCallSign(errors, qso.OperatorCallsign, false);

        ValidationRules.ValidateQslStatus(errors, qso.QslSent, false);
        ValidationRules.ValidateQslStatus(errors, qso.QslRcvd, false);
        ValidationRules.ValidateQslVia(errors, qso.QslSentVia, false);
        ValidationRules.ValidateQslVia(errors, qso.QslRcvdVia, false);

        if (qso.QslSentDate.HasValue && qso.QslSentDate.Value > DateTime.UtcNow.AddDays(1))
            errors.Add("QSL Sent Date cannot be in the future");

        if (qso.QslRcvdDate.HasValue && qso.QslRcvdDate.Value > DateTime.UtcNow.AddDays(1))
            errors.Add("QSL Received Date cannot be in the future");

        if (!string.IsNullOrWhiteSpace(qso.SiteComment) && qso.SiteComment.Length > 64)
            errors.Add("Site Comment cannot exceed 64 characters");

        if (!string.IsNullOrWhiteSpace(qso.Comment) && qso.Comment.Length > 4000)
            errors.Add("Comment cannot exceed 4000 characters");

        return errors;
    }
}
