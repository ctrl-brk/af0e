using System.Diagnostics;
using AF0E.DB.Models;
using Microsoft.EntityFrameworkCore;

namespace AF0E.DB;

public class HrdDbContext(string connectionString, QueryTrackingBehavior trackingBehavior = QueryTrackingBehavior.NoTracking) : DbContext
{
    public virtual DbSet<HrdLog> Log { get; set; }
    public virtual DbSet<PotaPark> PotaParks { get; set; }
    public virtual DbSet<PotaActivation> PotaActivations { get; set; }
    public virtual DbSet<PotaContact> PotaContacts { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseSqlServer(connectionString)
            .UseQueryTrackingBehavior(trackingBehavior)
#if DEBUG
            .EnableSensitiveDataLogging()
            .LogTo(e => Debug.WriteLine(e))
#endif
            ;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //modelBuilder.UseCollation("SQL_Latin1_General_CP1251_CI_AS");
        OnLogModelCreating(modelBuilder);
        OnParksModelCreating(modelBuilder);
        OnActivationsModelCreating(modelBuilder);
        OnContactsModelCreating(modelBuilder);
    }

    private static void OnContactsModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PotaContact>(entity =>
        {
            entity.HasKey(e => e.ContactId);
            entity.HasIndex(e => e.ActivationId, "IX_PotaContacts_ActivationId");
            entity.HasIndex(e => e.LogId, "IX_PotaContacts_LogId");

            entity.Property(e => e.P2P).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.Lat).HasColumnType("decimal(10, 6)");
            entity.Property(e => e.Long).HasColumnType("decimal(10, 6)");
            entity.Property(e => e.QrzGeoLoc).HasMaxLength(10).IsUnicode(false);

            entity.HasOne(d => d.Activation).WithMany(p => p.PotaContacts)
                .HasForeignKey(d => d.ActivationId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Log).WithMany(p => p.PotaContacts)
                .HasForeignKey(d => d.LogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PotaContacts_HrdLog_LogId");
        });
    }

    private static void OnActivationsModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PotaActivation>(entity =>
        {
            entity.HasKey(e => e.ActivationId);
            entity.HasIndex(e => e.ParkId, "IX_PotaActivations_ParkId");
            entity.HasIndex(e => e.StartDate, "IX_PotaActivations_StartDate");

            entity.Property(e => e.Grid).HasMaxLength(10).IsUnicode(false);
            entity.Property(e => e.County).HasMaxLength(100);
            entity.Property(e => e.County).HasMaxLength(200);
            entity.Property(e => e.State).HasMaxLength(2).IsFixedLength().IsUnicode(false);
            entity.Property(e => e.StartDate).HasColumnType("datetime");
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.Lat).HasColumnType("decimal(10,6)");
            entity.Property(e => e.Long).HasColumnType("decimal(10,6)");
            entity.Property(e => e.LogSubmittedDate).HasColumnType("datetime");
            entity.Property(e => e.SiteComments).IsUnicode(false);

            entity.HasOne(d => d.Park).WithMany(p => p.PotaActivations)
                .HasForeignKey(d => d.ParkId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });
    }

    private static void OnParksModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PotaPark>(entity =>
        {
            entity.HasKey(e => e.ParkId);
            entity.HasIndex(e => e.Country, "IX_PotaParks_Country");
            entity.HasIndex(e => e.ParkNum, "IX_PotaParks_ParkNum").IsUnique();

            entity.Property(e => e.ParkNum).HasMaxLength(20).IsUnicode(false);
            entity.Property(e => e.ParkName).HasMaxLength(500);
            entity.Property(e => e.Grid).HasMaxLength(10).IsUnicode(false);
            entity.Property(e => e.Lat).HasColumnType("decimal(7,4)");
            entity.Property(e => e.Long).HasColumnType("decimal(7,4)");
            entity.Property(e => e.Location).HasMaxLength(200).IsUnicode(false);
            entity.Property(e => e.Country).HasMaxLength(5).IsUnicode(false);

        });
    }

    private static void OnLogModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HrdLog>(entity =>
        {
            entity.ToTable("TABLE_HRD_CONTACTS_V01");
            entity.HasKey(e => e.ColPrimaryKey).HasName("PK__TABLE_HR__7DFAE7097F60ED59");
            entity.HasIndex(e => e.ColBand, "HRD_IDX_COL_BAND");
            entity.HasIndex(e => e.ColCall, "HRD_IDX_COL_CALL");
            entity.HasIndex(e => e.ColCont, "HRD_IDX_COL_CONT");
            entity.HasIndex(e => e.ColDxcc, "HRD_IDX_COL_DXCC");
            entity.HasIndex(e => e.ColIota, "HRD_IDX_COL_IOTA");
            entity.HasIndex(e => e.ColMode, "HRD_IDX_COL_MODE");
            entity.HasIndex(e => e.ColPfx, "HRD_IDX_COL_PFX");
            entity.HasIndex(e => e.ColTimeOn, "HRD_IDX_COL_TIME_ON");

            entity.Property(e => e.ColPrimaryKey).HasColumnName("COL_PRIMARY_KEY");
            entity.Property(e => e.ColAIndex).HasColumnName("COL_A_INDEX");
            entity.Property(e => e.ColAddress)
                .HasMaxLength(255)
                .HasColumnName("COL_ADDRESS");
            entity.Property(e => e.ColAge).HasColumnName("COL_AGE");
            entity.Property(e => e.ColAntAz).HasColumnName("COL_ANT_AZ");
            entity.Property(e => e.ColAntEl).HasColumnName("COL_ANT_EL");
            entity.Property(e => e.ColAntPath)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("COL_ANT_PATH");
            entity.Property(e => e.ColArrlSect)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("COL_ARRL_SECT");
            entity.Property(e => e.ColBand)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("COL_BAND");
            entity.Property(e => e.ColBandRx)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("COL_BAND_RX");
            entity.Property(e => e.ColBiography)
                .HasMaxLength(4096)
                .IsUnicode(false)
                .HasColumnName("COL_BIOGRAPHY");
            entity.Property(e => e.ColCall)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasColumnName("COL_CALL");
            entity.Property(e => e.ColCheck)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("COL_CHECK");
            entity.Property(e => e.ColClass)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("COL_CLASS");
            entity.Property(e => e.ColCnty)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasColumnName("COL_CNTY");
            entity.Property(e => e.ColComment)
                .HasMaxLength(4000)
                .HasColumnName("COL_COMMENT");
            entity.Property(e => e.ColCont)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("COL_CONT");
            entity.Property(e => e.ColContactedOp)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasColumnName("COL_CONTACTED_OP");
            entity.Property(e => e.ColContestId)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasColumnName("COL_CONTEST_ID");
            entity.Property(e => e.ColCountry)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("COL_COUNTRY");
            entity.Property(e => e.ColCqz).HasColumnName("COL_CQZ");
            entity.Property(e => e.ColCreditGranted)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("COL_CREDIT_GRANTED");
            entity.Property(e => e.ColCreditSubmitted)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("COL_CREDIT_SUBMITTED");
            entity.Property(e => e.ColDistance).HasColumnName("COL_DISTANCE");
            entity.Property(e => e.ColDxcc)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("COL_DXCC");
            entity.Property(e => e.ColEmail)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasColumnName("COL_EMAIL");
            entity.Property(e => e.ColEqCall)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasColumnName("COL_EQ_CALL");
            entity.Property(e => e.ColEqslQslRcvd)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("COL_EQSL_QSL_RCVD");
            entity.Property(e => e.ColEqslQslSent)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("COL_EQSL_QSL_SENT");
            entity.Property(e => e.ColEqslQslrdate)
                .HasColumnType("datetime")
                .HasColumnName("COL_EQSL_QSLRDATE");
            entity.Property(e => e.ColEqslQslsdate)
                .HasColumnType("datetime")
                .HasColumnName("COL_EQSL_QSLSDATE");
            entity.Property(e => e.ColEqslStatus)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("COL_EQSL_STATUS");
            entity.Property(e => e.ColForceInit).HasColumnName("COL_FORCE_INIT");
            entity.Property(e => e.ColFreq).HasColumnName("COL_FREQ");
            entity.Property(e => e.ColFreqRx).HasColumnName("COL_FREQ_RX");
            entity.Property(e => e.ColGridsquare)
                .HasMaxLength(12)
                .IsUnicode(false)
                .HasColumnName("COL_GRIDSQUARE");
            entity.Property(e => e.ColHeading).HasColumnName("COL_HEADING");
            entity.Property(e => e.ColHrdcountryno)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("COL_HRDCOUNTRYNO");
            entity.Property(e => e.ColIota)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("COL_IOTA");
            entity.Property(e => e.ColIsmultiplier)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("COL_ISMULTIPLIER");
            entity.Property(e => e.ColItuz).HasColumnName("COL_ITUZ");
            entity.Property(e => e.ColKIndex).HasColumnName("COL_K_INDEX");
            entity.Property(e => e.ColLat).HasColumnName("COL_LAT");
            entity.Property(e => e.ColLon).HasColumnName("COL_LON");
            entity.Property(e => e.ColLotwQslRcvd)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("COL_LOTW_QSL_RCVD");
            entity.Property(e => e.ColLotwQslSent)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("COL_LOTW_QSL_SENT");
            entity.Property(e => e.ColLotwQslrdate)
                .HasColumnType("datetime")
                .HasColumnName("COL_LOTW_QSLRDATE");
            entity.Property(e => e.ColLotwQslsdate)
                .HasColumnType("datetime")
                .HasColumnName("COL_LOTW_QSLSDATE");
            entity.Property(e => e.ColLotwStatus)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("COL_LOTW_STATUS");
            entity.Property(e => e.ColMaxBursts).HasColumnName("COL_MAX_BURSTS");
            entity.Property(e => e.ColMode)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("COL_MODE");
            entity.Property(e => e.ColMsShower)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasColumnName("COL_MS_SHOWER");
            entity.Property(e => e.ColMyCity)
                .HasMaxLength(32)
                .HasColumnName("COL_MY_CITY");
            entity.Property(e => e.ColMyCnty)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasColumnName("COL_MY_CNTY");
            entity.Property(e => e.ColMyCountry)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("COL_MY_COUNTRY");
            entity.Property(e => e.ColMyCqZone).HasColumnName("COL_MY_CQ_ZONE");
            entity.Property(e => e.ColMyGridsquare)
                .HasMaxLength(12)
                .IsUnicode(false)
                .HasColumnName("COL_MY_GRIDSQUARE");
            entity.Property(e => e.ColMyIota)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("COL_MY_IOTA");
            entity.Property(e => e.ColMyItuZone).HasColumnName("COL_MY_ITU_ZONE");
            entity.Property(e => e.ColMyLat).HasColumnName("COL_MY_LAT");
            entity.Property(e => e.ColMyLon).HasColumnName("COL_MY_LON");
            entity.Property(e => e.ColMyName)
                .HasMaxLength(64)
                .HasColumnName("COL_MY_NAME");
            entity.Property(e => e.ColMyPostalCode)
                .HasMaxLength(24)
                .IsUnicode(false)
                .HasColumnName("COL_MY_POSTAL_CODE");
            entity.Property(e => e.ColMyRig)
                .HasMaxLength(255)
                .HasColumnName("COL_MY_RIG");
            entity.Property(e => e.ColMySig)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasColumnName("COL_MY_SIG");
            entity.Property(e => e.ColMySigInfo)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("COL_MY_SIG_INFO");
            entity.Property(e => e.ColMyState)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasColumnName("COL_MY_STATE");
            entity.Property(e => e.ColMyStreet)
                .HasMaxLength(64)
                .HasColumnName("COL_MY_STREET");
            entity.Property(e => e.ColName)
                .HasMaxLength(128)
                .HasColumnName("COL_NAME");
            entity.Property(e => e.ColNotes)
                .HasMaxLength(4000)
                .HasColumnName("COL_NOTES");
            entity.Property(e => e.ColNrBursts).HasColumnName("COL_NR_BURSTS");
            entity.Property(e => e.ColNrPings).HasColumnName("COL_NR_PINGS");
            entity.Property(e => e.ColOperator)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasColumnName("COL_OPERATOR");
            entity.Property(e => e.ColOwnerCallsign)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasColumnName("COL_OWNER_CALLSIGN");
            entity.Property(e => e.ColPfx)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasColumnName("COL_PFX");
            entity.Property(e => e.ColPrecedence)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasColumnName("COL_PRECEDENCE");
            entity.Property(e => e.ColPropMode)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("COL_PROP_MODE");
            entity.Property(e => e.ColPublicKey)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("COL_PUBLIC_KEY");
            entity.Property(e => e.ColQslRcvd)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("COL_QSL_RCVD");
            entity.Property(e => e.ColQslRcvdVia)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("COL_QSL_RCVD_VIA");
            entity.Property(e => e.ColQslSent)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("COL_QSL_SENT");
            entity.Property(e => e.ColQslSentVia)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("COL_QSL_SENT_VIA");
            entity.Property(e => e.ColQslVia)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("COL_QSL_VIA");
            entity.Property(e => e.ColQslmsg)
                .HasMaxLength(255)
                .HasColumnName("COL_QSLMSG");
            entity.Property(e => e.ColQslrdate)
                .HasColumnType("datetime")
                .HasColumnName("COL_QSLRDATE");
            entity.Property(e => e.ColQslsdate)
                .HasColumnType("datetime")
                .HasColumnName("COL_QSLSDATE");
            entity.Property(e => e.ColQsoComplete)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("COL_QSO_COMPLETE");
            entity.Property(e => e.ColQsoRandom).HasColumnName("COL_QSO_RANDOM");
            entity.Property(e => e.ColQth)
                .HasMaxLength(64)
                .HasColumnName("COL_QTH");
            entity.Property(e => e.ColRig)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("COL_RIG");
            entity.Property(e => e.ColRoverlocation)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("COL_ROVERLOCATION");
            entity.Property(e => e.ColRstRcvd)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasColumnName("COL_RST_RCVD");
            entity.Property(e => e.ColRstSent)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasColumnName("COL_RST_SENT");
            entity.Property(e => e.ColRxPwr).HasColumnName("COL_RX_PWR");
            entity.Property(e => e.ColSatMode)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasColumnName("COL_SAT_MODE");
            entity.Property(e => e.ColSatName)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasColumnName("COL_SAT_NAME");
            entity.Property(e => e.ColSfi).HasColumnName("COL_SFI");
            entity.Property(e => e.ColSig)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasColumnName("COL_SIG");
            entity.Property(e => e.ColSigInfo)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("COL_SIG_INFO");
            entity.Property(e => e.ColSrx).HasColumnName("COL_SRX");
            entity.Property(e => e.ColSrxString)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasColumnName("COL_SRX_STRING");
            entity.Property(e => e.ColState)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasColumnName("COL_STATE");
            entity.Property(e => e.ColStationCallsign)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasColumnName("COL_STATION_CALLSIGN");
            entity.Property(e => e.ColStx).HasColumnName("COL_STX");
            entity.Property(e => e.ColStxString)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasColumnName("COL_STX_STRING");
            entity.Property(e => e.ColSubmode)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("COL_SUBMODE");
            entity.Property(e => e.ColSwl).HasColumnName("COL_SWL");
            entity.Property(e => e.ColTenTen).HasColumnName("COL_TEN_TEN");
            entity.Property(e => e.ColTimeOff)
                .HasColumnType("datetime")
                .HasColumnName("COL_TIME_OFF");
            entity.Property(e => e.ColTimeOn)
                .HasColumnType("datetime")
                .HasColumnName("COL_TIME_ON");
            entity.Property(e => e.ColTxPwr).HasColumnName("COL_TX_PWR");
            entity.Property(e => e.ColUserDefined0)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("COL_USER_DEFINED_0");
            entity.Property(e => e.ColUserDefined1)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("COL_USER_DEFINED_1");
            entity.Property(e => e.ColUserDefined2)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("COL_USER_DEFINED_2");
            entity.Property(e => e.ColUserDefined3)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("COL_USER_DEFINED_3");
            entity.Property(e => e.ColUserDefined4)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("COL_USER_DEFINED_4");
            entity.Property(e => e.ColUserDefined5)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("COL_USER_DEFINED_5");
            entity.Property(e => e.ColUserDefined6)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("COL_USER_DEFINED_6");
            entity.Property(e => e.ColUserDefined7)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("COL_USER_DEFINED_7");
            entity.Property(e => e.ColUserDefined8)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("COL_USER_DEFINED_8");
            entity.Property(e => e.ColUserDefined9)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("COL_USER_DEFINED_9");
            entity.Property(e => e.ColWeb)
                .HasMaxLength(128)
                .IsUnicode(false)
                .HasColumnName("COL_WEB");
        });
    }
}
