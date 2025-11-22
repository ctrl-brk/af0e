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
        log.ColRstSent = qso.RstSent;
        log.ColRstRcvd = qso.RstRcvd;
        log.ColMyCity = qso.MyCity;
        log.ColMyCnty = qso.MyCounty;
        log.ColMyState = qso.MyState;
        log.ColMyCountry = qso.MyCountry;
        log.ColMyCqZone = qso.MyCqZone;
        log.ColMyItuZone = qso.MyItuZone;
        log.ColMyGridsquare = qso.MyGrid;
        log.ColQslSent = qso.QslSent;
        log.ColQslsdate = qso.QslSentDate;
        log.ColQslSentVia = qso.QslSentVia;
        log.ColQslRcvd = qso.QslRcvd;
        log.ColQslrdate = qso.QslRcvdDate;
        log.ColQslRcvdVia = qso.QslRcvdVia;
        log.SiteComment = qso.SiteComment;

        if (includeAdminFields)
        {
            log.ColComment = qso.Comment;
        }
    }
}
