using AF0E.DB.Models;
using Logbook.Api.Models;
using Logbook.Api.Responses;

namespace Logbook.Api.Extensions;

public static class HrdLogExtensions
{
    /// <param name="log">The HrdLog entity to update</param>
    extension(HrdLog log)
    {
        /// <summary>
        /// Updates the HrdLog entity with values from QsoDetails
        /// </summary>
        /// <param name="qso">The QsoDetails with updated values</param>
        /// <param name="includeAdminFields">Whether to update admin-only fields (like Comment)</param>
        public void UpdateFromQsoDetails(QsoDetails qso, bool includeAdminFields = false)
        {
            log.ColCall = qso.Call;
            log.ColTimeOn = qso.Date;
            log.ColBand = qso.Band;
            log.ColFreq = qso.Freq;
            log.ColMode = qso.Mode;
            log.ColRstSent = N(qso.RstSent);
            log.ColRstRcvd = N(qso.RstRcvd);
            log.ColName = N(qso.Name);
            log.ColCnty = N(qso.County);
            log.ColState = N(qso.State)?.ToUpperInvariant();
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

        public void UpdateFromQrzLookup(QrzResponse qrz)
        {
            if (qrz.notFound || qrz.qrzResult is null)
                return;

            var qrzResult = qrz.qrzResult.Callsign;

            log.ColName ??= qrzResult.name_fmt;
            log.ColAddress ??= qrzResult.addr1 + " " + qrzResult.addr2;
            log.ColCnty ??= qrzResult.county;
            log.ColState ??= qrzResult.state;
            log.ColCountry ??= qrzResult.country;
            log.ColGridsquare ??= qrzResult.grid;
            log.ColCqz ??= qrzResult.cqzone;
            log.ColItuz ??= qrzResult.ituzone;
            if (log.ColDxcc is null or "0")  log.ColDxcc = qrzResult.dxcc.ToString();
            log.ColHrdcountryno ??= qrzResult.dxcc.ToString();
            log.ColEmail ??= qrzResult.email;
            log.ColLat ??= (double)qrzResult.lat;
            log.ColLon ??= (double)qrzResult.lon;
        }
    }

    private static string? N(string? value, string? def = null) => string.IsNullOrWhiteSpace(value) ? def : value;
}
