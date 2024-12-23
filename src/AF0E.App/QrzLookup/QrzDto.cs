using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming
// ReSharper disable CommentTypo

namespace QrzLookup;

[XmlRoot("QRZDatabase", Namespace = "http://xmldata.qrz.com")]
#pragma warning disable CA1515
public class QRZDatabase
{
    [XmlAttribute("version")]
    public string? Version { get; set; }
    [XmlElement("Callsign")]
    public Callsign Callsign { get; set; } = null!;
    [XmlElement("Session")]
    public Session Session { get; set; } = null!;
}

[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
[SuppressMessage("ReSharper", "IdentifierTypo")]
public class Callsign
{
    public string call { get; set; } = null!;
    public string? aliases { get; set; }
    public string? xref { get; set; } //Cross-reference: the query callsign that returned this record
    public int dxcc { get; set; }
    public string? fname { get; set; }
    public string? name { get; set; }
    public string? nickname { get; set; }
    public string? name_fmt { get; set; } //Combined full name and nickname in the format used by QRZ. This fortmat is subject to change.
    public string? addr1 { get; set; }
    public string? addr2 { get; set; }
    public string? state { get; set; }
    public string? zip { get; set; }
    public string? country { get; set; }
    public ushort ccode { get; set; } //dxcc entity code for the mailing address country
    public decimal lat { get; set; }
    public decimal lon { get; set; }
    public string? grid { get; set; }
    public string? county { get; set; } //USA
    public uint fips { get; set; } //FIPS county identifier (USA)
    public string? land { get; set; } //DXCC country name of the callsign
    public string? efdate { get; set; } //license effective date (USA)
    public string? expdate { get; set; } //license expiration date (USA)
    public string? p_call { get; set; } //previous callsign
    public string? @class { get; set; } //license class
    public string? codes { get; set; } //license type codes (USA)
    public string? qslmgr { get; set; }
    public string? email { get; set; }
    [SuppressMessage("Design", "CA1056:URI-like properties should not be strings")]
    public string? url { get; set; }
    public uint u_views { get; set; } //QRZ web page views
    public string? bio { get; set; }  //approximate length of the bio HTML in bytes <bio>3937/2003-11-04</bio>
    public string? biodate { get; set; }  //date of the last bio update
    public string? image { get; set; }
    public string? imageinfo { get; set; } //height:width:size in bytes, of the image file <imageinfo>509:800:137748</imageinfo>
    public uint serial { get; set; } //QRZ db serial number
    public string? moddate { get; set; } //QRZ callsign last modified date
    public uint MSA { get; set; } //Metro Service Area (USPS)
    public string? AreaCode { get; set; } //Telephone Area Code (USA)
    public string? TimeZone { get; set; } //Time Zone (USA)
    public sbyte GMTOffset { get; set; }
    public string? DST { get; set; }
    public string? eqsl { get; set; } //Will accept e-qsl (0/1 or blank if unknown)
    public string? mqsl { get; set; } //Will return paper QSL (0/1 or blank if unknown)
    public string? lotw { get; set; } //Will accept LOTW (0/1 or blank if unknown)
    public byte cqzone { get; set; }
    public byte ituzone { get; set; }
    public string? iota { get; set; }
    /*
    The geoloc field describes the source of the returned lat/long data. The possible string values for geoloc are:

    user - the value was input by the user
    geocode - the value was derived from the USA Geocoding data
    grid - the value was derived from a user supplied grid square
    zip - the value was derived from the callsign's USA Zip Code
    state - the value was derived from the callsign's USA State
    dxcc - the value was derived from the callsign's DXCC entity (country)
    none - no value could be determined
    */
    public string? geoloc { get; set; }//Describes source of lat/long data
    public string? attn { get; set; }
    public ushort born { get; set; }
    public string? user { get; set; } //User who manages this callsign on QRZ
}

// ReSharper disable once ClassNeverInstantiated.Global
public class Session
{
    public string? Key { get; set; }
    public ushort Count { get; set; }
    public string? Error { get; set; }
    public string? SubExp { get; set; }
    public string? GMTime { get; set; }
}
