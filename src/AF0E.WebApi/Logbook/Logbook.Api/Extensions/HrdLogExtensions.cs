using AF0E.DB.Models;
using Logbook.Api.Models;

namespace Logbook.Api.Extensions;

public static class HrdLogExtensions
{
    /// <summary>
    /// Updates the HrdLog entity with values from QsoDetails
    /// </summary>
    /// <param name="log">The HrdLog entity to update</param>
    /// <param name="qso">The QsoDetails with updated values</param>
    /// <param name="includeAdminFields">Whether to update admin-only fields (like Comment)</param>
    public static void UpdateFromQsoDetails(this HrdLog log, QsoDetails qso, bool includeAdminFields = false)
    {
        log.ColCall = qso.Call;
        log.ColTimeOn = qso.Date;
        log.ColBand = qso.Band;
        log.ColFreq = qso.Freq;
        log.ColMode = qso.Mode;
        log.ColRstSent = N(qso.RstSent);
        log.ColRstRcvd = N(qso.RstRcvd);
        log.ColCnty = N(qso.County);
        log.ColCountry = N(qso.Country);
        log.ColGridsquare = N(qso.Grid);
        log.ColCqz = qso.CqZone;
        log.ColItuz = qso.ItuZone;
        log.ColMyCity = N(qso.MyCity);
        log.ColDxcc = qso.Dxcc.ToString();
        log.ColMyCnty = N(qso.MyCounty);
        log.ColMyState = N(qso.MyState);
        log.ColMyCountry = N(qso.MyCountry);
        log.ColMyCqZone = qso.MyCqZone;
        log.ColMyItuZone = qso.MyItuZone;
        log.ColMyGridsquare = N(qso.MyGrid);
        log.ColQslSent = N(qso.QslSent, "N");
        log.ColQslsdate = qso.QslSentDate;
        log.ColQslSentVia = N(qso.QslSentVia);
        log.ColQslRcvd = N(qso.QslRcvd, "N");
        log.ColQslrdate = qso.QslRcvdDate;
        log.ColQslRcvdVia = N(qso.QslRcvdVia);
        log.SiteComment = N(qso.SiteComment);

        if (includeAdminFields)
        {
            log.ColComment = qso.Comment;
        }
    }

    private static string? N(string? value, string? def = null) => string.IsNullOrWhiteSpace(value) ? def : value;
}
