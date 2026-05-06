// ReSharper disable PropertyCanBeMadeInitOnly.Global
namespace AF0E.DB.Models;

using System.Reflection;
using System.Collections.Generic;

public sealed class HrdLog
{
    /// <summary>
    /// Initializes class with default values for HRD logbook. It doesn't handle nulls well.
    /// </summary>
    /// <remarks>
    /// If adding/changing fields, check TryCreateLogEntry in LogBookHandlers.cs (Logbook.Api)
    /// </remarks>
    public HrdLog()
    {
        ColAge = ColAIndex = ColAntAz = ColAntEl = ColCqz = ColDistance = ColForceInit = ColFreqRx = ColDistance = 0;
        ColHeading = ColKIndex = ColLat = ColMyLat = ColLon = ColMyLon = ColMaxBursts = ColNrBursts = ColNrPings = 0;
        ColQsoRandom = ColRxPwr = ColSfi = ColSrx = ColStx = ColSwl = ColTenTen = ColForceInit = ColTxPwr = 0;
        ColEqslQslRcvd = ColEqslQslSent = ColLotwQslRcvd = ColLotwQslSent = "N";
        ColCqz = ColItuz = ColMyCqZone = ColMyItuZone = 0;
        ColDxcc = "0";
    }

public int ColPrimaryKey { get; set; }
    public string? ColAddress { get; set; }
    public double? ColAge { get; set; }
    public double? ColAIndex { get; set; }
    public double? ColAntAz { get; set; }
    public double? ColAntEl { get; set; }
    public string? ColAntPath { get; set; }
    public string? ColArrlSect { get; set; }
    public string? ColBand { get; set; }
    public string? ColBandRx { get; set; }
    public string? ColBiography { get; set; }
    public string ColCall { get; set; } = null!;
    public string? ColCheck { get; set; }
    public string? ColClass { get; set; }
    public string? ColCnty { get; set; }
    public string? ColComment { get; set; }
    public string? ColCont { get; set; }
    public string? ColContactedOp { get; set; }
    public string? ColContestId { get; set; }
    public string? ColCountry { get; set; }
    public double? ColCqz { get; set; }
    public double? ColDistance { get; set; }
    public string? ColDxcc { get; set; }
    public string? ColEmail { get; set; }
    public string? ColEqCall { get; set; }
    public DateTime? ColEqslQslrdate { get; set; }
    public DateTime? ColEqslQslsdate { get; set; }
    public string? ColEqslQslRcvd { get; set; }
    public string? ColEqslQslSent { get; set; }
    public string? ColEqslStatus { get; set; }
    public double? ColForceInit { get; set; }
    public double? ColFreq { get; set; }
    public double? ColFreqRx { get; set; }
    public string? ColGridsquare { get; set; }
    public double? ColHeading { get; set; }
    public string? ColIota { get; set; }
    public double? ColItuz { get; set; }
    public double? ColKIndex { get; set; }
    public double? ColLat { get; set; }
    public double? ColLon { get; set; }
    public DateTime? ColLotwQslrdate { get; set; }
    public DateTime? ColLotwQslsdate { get; set; }
    public string? ColLotwQslRcvd { get; set; }
    public string? ColLotwQslSent { get; set; }
    public string? ColLotwStatus { get; set; }
    public double? ColMaxBursts { get; set; }
    public string? ColMode { get; set; }
    public string? ColMsShower { get; set; }
    public string? ColMyCity { get; set; }
    public string? ColMyCnty { get; set; }
    public string? ColMyCountry { get; set; }
    public double? ColMyCqZone { get; set; }
    public string? ColMyGridsquare { get; set; }
    public string? ColMyIota { get; set; }
    public double? ColMyItuZone { get; set; }
    public double? ColMyLat { get; set; }
    public double? ColMyLon { get; set; }
    public string? ColMyName { get; set; }
    public string? ColMyPostalCode { get; set; }
    public string? ColMyRig { get; set; }
    public string? ColMySig { get; set; }
    public string? ColMySigInfo { get; set; }
    public string? ColMyState { get; set; }
    public string? ColMyStreet { get; set; }
    public string? ColName { get; set; }
    public string? ColNotes { get; set; }
    public double? ColNrBursts { get; set; }
    public double? ColNrPings { get; set; }
    public string? ColOperator { get; set; }
    public string? ColOwnerCallsign { get; set; }
    public string? ColPfx { get; set; }
    public string? ColPrecedence { get; set; }
    public string? ColPropMode { get; set; }
    public string? ColPublicKey { get; set; }
    public string? ColQslmsg { get; set; }
    public DateTime? ColQslrdate { get; set; }
    public DateTime? ColQslsdate { get; set; }
    public string? ColQslRcvd { get; set; }
    public string? ColQslRcvdVia { get; set; }
    public string? ColQslSent { get; set; }
    public string? ColQslSentVia { get; set; }
    public string? ColQslVia { get; set; }
    public string? ColQsoComplete { get; set; }
    public double? ColQsoRandom { get; set; }
    public string? ColQth { get; set; }
    public string? ColRig { get; set; }
    public string? ColRstRcvd { get; set; }
    public string? ColRstSent { get; set; }
    public double? ColRxPwr { get; set; }
    public string? ColSatMode { get; set; }
    public string? ColSatName { get; set; }
    public double? ColSfi { get; set; }
    public string? ColSig { get; set; }
    public string? ColSigInfo { get; set; }
    public double? ColSrx { get; set; }
    public string? ColSrxString { get; set; }
    public string? ColState { get; set; }
    public string? ColStationCallsign { get; set; }
    public double? ColStx { get; set; }
    public string? ColStxString { get; set; }
    public double? ColSwl { get; set; }
    public double? ColTenTen { get; set; }
    public DateTime? ColTimeOff { get; set; }
    public DateTime? ColTimeOn { get; set; }
    public double? ColTxPwr { get; set; }
    public string? ColWeb { get; set; }
    public string? SiteComment { get; set; }
    public string? QslMgrCall { get; set; }
    public string? QslComment { get; set; }
    public string? Metadata { get; set; }
    public string? ColUserDefined4 { get; set; }
    public string? ColUserDefined5 { get; set; }
    public string? ColUserDefined6 { get; set; }
    public string? ColUserDefined7 { get; set; }
    public string? ColUserDefined8 { get; set; }
    public string? ColUserDefined9 { get; set; }
    public string? ColCreditGranted { get; set; }
    public string? ColCreditSubmitted { get; set; }
    public string? ColIsmultiplier { get; set; }
    public string? ColRoverlocation { get; set; }
    public string? ColHrdcountryno { get; set; }
    public string? ColSubmode { get; set; }

#pragma warning disable CA2227
    public ICollection<PotaContact> PotaContacts { get; set; } = [];
    public ICollection<PotaHunting> PotaHunting { get; set; } = [];
#pragma warning restore CA2227

    /// <summary>Maps property names to their actual database column names. ColPrimaryKey is intentionally excluded.</summary>
    private static readonly Dictionary<string, string> _columnMap = new()
    {
        { nameof(ColAddress),          "COL_ADDRESS" },
        { nameof(ColAge),              "COL_AGE" },
        { nameof(ColAIndex),           "COL_A_INDEX" },
        { nameof(ColAntAz),            "COL_ANT_AZ" },
        { nameof(ColAntEl),            "COL_ANT_EL" },
        { nameof(ColAntPath),          "COL_ANT_PATH" },
        { nameof(ColArrlSect),         "COL_ARRL_SECT" },
        { nameof(ColBand),             "COL_BAND" },
        { nameof(ColBandRx),           "COL_BAND_RX" },
        { nameof(ColBiography),        "COL_BIOGRAPHY" },
        { nameof(ColCall),             "COL_CALL" },
        { nameof(ColCheck),            "COL_CHECK" },
        { nameof(ColClass),            "COL_CLASS" },
        { nameof(ColCnty),             "COL_CNTY" },
        { nameof(ColComment),          "COL_COMMENT" },
        { nameof(ColCont),             "COL_CONT" },
        { nameof(ColContactedOp),      "COL_CONTACTED_OP" },
        { nameof(ColContestId),        "COL_CONTEST_ID" },
        { nameof(ColCountry),          "COL_COUNTRY" },
        { nameof(ColCqz),              "COL_CQZ" },
        { nameof(ColCreditGranted),    "COL_CREDIT_GRANTED" },
        { nameof(ColCreditSubmitted),  "COL_CREDIT_SUBMITTED" },
        { nameof(ColDistance),         "COL_DISTANCE" },
        { nameof(ColDxcc),             "COL_DXCC" },
        { nameof(ColEmail),            "COL_EMAIL" },
        { nameof(ColEqCall),           "COL_EQ_CALL" },
        { nameof(ColEqslQslRcvd),      "COL_EQSL_QSL_RCVD" },
        { nameof(ColEqslQslSent),      "COL_EQSL_QSL_SENT" },
        { nameof(ColEqslQslrdate),     "COL_EQSL_QSLRDATE" },
        { nameof(ColEqslQslsdate),     "COL_EQSL_QSLSDATE" },
        { nameof(ColEqslStatus),       "COL_EQSL_STATUS" },
        { nameof(ColForceInit),        "COL_FORCE_INIT" },
        { nameof(ColFreq),             "COL_FREQ" },
        { nameof(ColFreqRx),           "COL_FREQ_RX" },
        { nameof(ColGridsquare),       "COL_GRIDSQUARE" },
        { nameof(ColHeading),          "COL_HEADING" },
        { nameof(ColHrdcountryno),     "COL_HRDCOUNTRYNO" },
        { nameof(ColIota),             "COL_IOTA" },
        { nameof(ColIsmultiplier),     "COL_ISMULTIPLIER" },
        { nameof(ColItuz),             "COL_ITUZ" },
        { nameof(ColKIndex),           "COL_K_INDEX" },
        { nameof(ColLat),              "COL_LAT" },
        { nameof(ColLon),              "COL_LON" },
        { nameof(ColLotwQslRcvd),      "COL_LOTW_QSL_RCVD" },
        { nameof(ColLotwQslSent),      "COL_LOTW_QSL_SENT" },
        { nameof(ColLotwQslrdate),     "COL_LOTW_QSLRDATE" },
        { nameof(ColLotwQslsdate),     "COL_LOTW_QSLSDATE" },
        { nameof(ColLotwStatus),       "COL_LOTW_STATUS" },
        { nameof(ColMaxBursts),        "COL_MAX_BURSTS" },
        { nameof(ColMode),             "COL_MODE" },
        { nameof(ColMsShower),         "COL_MS_SHOWER" },
        { nameof(ColMyCity),           "COL_MY_CITY" },
        { nameof(ColMyCnty),           "COL_MY_CNTY" },
        { nameof(ColMyCountry),        "COL_MY_COUNTRY" },
        { nameof(ColMyCqZone),         "COL_MY_CQ_ZONE" },
        { nameof(ColMyGridsquare),     "COL_MY_GRIDSQUARE" },
        { nameof(ColMyIota),           "COL_MY_IOTA" },
        { nameof(ColMyItuZone),        "COL_MY_ITU_ZONE" },
        { nameof(ColMyLat),            "COL_MY_LAT" },
        { nameof(ColMyLon),            "COL_MY_LON" },
        { nameof(ColMyName),           "COL_MY_NAME" },
        { nameof(ColMyPostalCode),     "COL_MY_POSTAL_CODE" },
        { nameof(ColMyRig),            "COL_MY_RIG" },
        { nameof(ColMySig),            "COL_MY_SIG" },
        { nameof(ColMySigInfo),        "COL_MY_SIG_INFO" },
        { nameof(ColMyState),          "COL_MY_STATE" },
        { nameof(ColMyStreet),         "COL_MY_STREET" },
        { nameof(ColName),             "COL_NAME" },
        { nameof(ColNotes),            "COL_NOTES" },
        { nameof(ColNrBursts),         "COL_NR_BURSTS" },
        { nameof(ColNrPings),          "COL_NR_PINGS" },
        { nameof(ColOperator),         "COL_OPERATOR" },
        { nameof(ColOwnerCallsign),    "COL_OWNER_CALLSIGN" },
        { nameof(ColPfx),              "COL_PFX" },
        { nameof(ColPrecedence),       "COL_PRECEDENCE" },
        { nameof(ColPropMode),         "COL_PROP_MODE" },
        { nameof(ColPublicKey),        "COL_PUBLIC_KEY" },
        { nameof(ColQslmsg),           "COL_QSLMSG" },
        { nameof(ColQslrdate),         "COL_QSLRDATE" },
        { nameof(ColQslsdate),         "COL_QSLSDATE" },
        { nameof(ColQslRcvd),          "COL_QSL_RCVD" },
        { nameof(ColQslRcvdVia),       "COL_QSL_RCVD_VIA" },
        { nameof(ColQslSent),          "COL_QSL_SENT" },
        { nameof(ColQslSentVia),       "COL_QSL_SENT_VIA" },
        { nameof(ColQslVia),           "COL_QSL_VIA" },
        { nameof(ColQsoComplete),      "COL_QSO_COMPLETE" },
        { nameof(ColQsoRandom),        "COL_QSO_RANDOM" },
        { nameof(ColQth),              "COL_QTH" },
        { nameof(ColRig),              "COL_RIG" },
        { nameof(ColRoverlocation),    "COL_ROVERLOCATION" },
        { nameof(ColRstRcvd),          "COL_RST_RCVD" },
        { nameof(ColRstSent),          "COL_RST_SENT" },
        { nameof(ColRxPwr),            "COL_RX_PWR" },
        { nameof(ColSatMode),          "COL_SAT_MODE" },
        { nameof(ColSatName),          "COL_SAT_NAME" },
        { nameof(ColSfi),              "COL_SFI" },
        { nameof(ColSig),              "COL_SIG" },
        { nameof(ColSigInfo),          "COL_SIG_INFO" },
        { nameof(ColSrx),              "COL_SRX" },
        { nameof(ColSrxString),        "COL_SRX_STRING" },
        { nameof(ColState),            "COL_STATE" },
        { nameof(ColStationCallsign),  "COL_STATION_CALLSIGN" },
        { nameof(ColStx),              "COL_STX" },
        { nameof(ColStxString),        "COL_STX_STRING" },
        { nameof(ColSubmode),          "COL_SUBMODE" },
        { nameof(ColSwl),              "COL_SWL" },
        { nameof(ColTenTen),           "COL_TEN_TEN" },
        { nameof(ColTimeOff),          "COL_TIME_OFF" },
        { nameof(ColTimeOn),           "COL_TIME_ON" },
        { nameof(ColTxPwr),            "COL_TX_PWR" },
        { nameof(ColWeb),              "COL_WEB" },
        { nameof(SiteComment),         "COL_USER_DEFINED_0" },
        { nameof(QslMgrCall),          "COL_USER_DEFINED_1" },
        { nameof(QslComment),          "COL_USER_DEFINED_2" },
        { nameof(Metadata),            "COL_USER_DEFINED_3" },
        { nameof(ColUserDefined4),     "COL_USER_DEFINED_4" },
        { nameof(ColUserDefined5),     "COL_USER_DEFINED_5" },
        { nameof(ColUserDefined6),     "COL_USER_DEFINED_6" },
        { nameof(ColUserDefined7),     "COL_USER_DEFINED_7" },
        { nameof(ColUserDefined8),     "COL_USER_DEFINED_8" },
        { nameof(ColUserDefined9),     "COL_USER_DEFINED_9" },
    };

    /// <summary>
    /// Generates a SQL INSERT statement with parameterized values.
    /// </summary>
    /// <param name="tableName">The name of the table to insert into (default: "HrdLog")</param>
    /// <returns>A tuple containing the SQL INSERT statement and a dictionary of parameter names and values</returns>
    /// <example>
    /// <code>
    /// // Create and populate an HrdLog instance
    /// var hrdLog = new HrdLog
    /// {
    ///     ColCall = "W5ABC",
    ///     ColName = "John Doe",
    ///     ColQth = "Texas",
    ///     ColBand = "40M",
    ///     ColMode = "CW",
    ///     ColFreq = 7.040,
    ///     ColRstSent = "599",
    ///     ColRstRcvd = "599",
    ///     ColTimeOn = DateTime.Now.AddHours(-1),
    ///     ColTimeOff = DateTime.Now,
    ///     ColComment = "Great signal!",
    ///     ColRig = "IC-7300",
    ///     ColMyName = "Jane Smith"
    /// };
    ///
    /// // Generate the SQL INSERT statement
    /// var (sql, parameters) = hrdLog.CreateInsertStatement("HrdLog");
    ///
    /// // Use with SqlConnection and SqlCommand
    /// using (SqlConnection connection = new SqlConnection(connectionString))
    /// {
    ///     connection.Open();
    ///
    ///     using (SqlCommand command = new SqlCommand(sql, connection))
    ///     {
    ///         // Add all parameters to the command
    ///         foreach (var param in parameters)
    ///         {
    ///             command.Parameters.AddWithValue(param.Key, param.Value);
    ///         }
    ///
    ///         // Execute the insert
    ///         int rowsAffected = command.ExecuteNonQuery();
    ///         Console.WriteLine($"Inserted {rowsAffected} row(s)");
    ///     }
    /// }
    /// </code>
    /// </example>
    public (string sql, Dictionary<string, object?>) CreateInsertStatement(string tableName = "TABLE_HRD_CONTACTS_V01")
    {
        var parameters = new Dictionary<string, object?>();
        var columnNames = new List<string>();
        var parameterNames = new List<string>();

        var properties = this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        var parameterIndex = 0;
        foreach (var property in properties)
        {
            if (!_columnMap.TryGetValue(property.Name, out var columnName))
                continue;

            var value = property.GetValue(this);

            columnNames.Add($"[{columnName}]");
            var paramName = $"@p{parameterIndex}";
            parameterNames.Add(paramName);
            parameters[paramName] = value ?? DBNull.Value;
            parameterIndex++;
        }

        var columnList = string.Join(", ", columnNames);
        var valuesList = string.Join(", ", parameterNames);

        var sql = $"INSERT INTO [{tableName}] ({columnList}) VALUES ({valuesList});";

        return (sql, parameters);
    }

    /// <summary>
    /// Generates a SQL INSERT statement with literal values (for debugging/logging only - NOT for actual execution).
    /// WARNING: This method does not properly escape values and is vulnerable to SQL injection. Use only for display purposes.
    /// </summary>
    /// <param name="tableName">The name of the table to insert into (default: "HrdLog")</param>
    /// <returns>A SQL INSERT statement string with literal values</returns>
    public string CreateInsertStatementWithLiterals(string tableName = "TABLE_HRD_CONTACTS_V01")
    {
        var columnNames = new List<string>();
        var values = new List<string>();

        var properties = this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            if (!_columnMap.TryGetValue(property.Name, out var columnName))
                continue;

            var value = property.GetValue(this);

            columnNames.Add($"[{columnName}]");

            if (value == null)
            {
                values.Add("NULL");
            }
            else if (property.PropertyType == typeof(string))
            {
                var escaped = ((string)value).Replace("'", "''");
                values.Add($"'{escaped}'");
            }
            else if (property.PropertyType == typeof(DateTime) || property.PropertyType == typeof(DateTime?))
            {
                values.Add($"'{((DateTime)value):yyyy-MM-dd HH:mm:ss.fff}'");
            }
            else if (property.PropertyType == typeof(bool) || property.PropertyType == typeof(bool?))
            {
                values.Add(((bool)value) ? "1" : "0");
            }
            else
            {
                values.Add(value.ToString()!);
            }
        }

        var columnList = string.Join(", ", columnNames);
        var valuesList = string.Join(", ", values);

        return $"INSERT INTO [{tableName}] ({columnList}){Environment.NewLine}  VALUES ({valuesList});";
    }
}
