// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global
namespace HamMarket;

public class SmtpSettings
{
    public bool Enabled { get; set; }
    public string SmtpServer { get; set; }
    public int Port { get; set; }
    public string User { get; set; }
    public string Password { get; set; }
}

public class SendGridSettings
{
    public bool Enabled { get; set; }
    public string ApiKey { get; set; }
}

public class EmailSettings
{
    public SmtpSettings Smtp { get; set; }
    public SendGridSettings SendGrid { get; set; }
    public string From { get; set; }
    public string To { get; set; }
    public string SubjectResultsFormat { get; set; }
    public string SubjectEmptyFormat { get; set; }
    /// <summary>
    /// File name to save generated html for an email/external access
    /// </summary>
    public string BodyFileName { get; set; }
    /// <summary>
    /// Whether to attach email body also as a html file
    /// </summary>
    public bool AttachFile { get; set; }
}

public class KeywordSearch
{
    public string Keywords { get; set; }
    public int MaxPosts { get; set; }
    public string ResultFile { get; set; }
}

public class CategorySearch
{
    public string Categories { get; set; }
    public int MaxPosts { get; set; }
    public string ResultFile { get; set; }
}

public class Cache
{ 
    /// <summary>
    /// Will be combined with ResourceUrl in the email and with ResourceFolder for storage
    /// </summary>
    /// <value>Where to cache images</value>
    public string ImageFolder { get; set; }
}

public class QthCom
{
    public string Title { get; set; }
    public KeywordSearch KeywordSearch { get; set; }
    public CategorySearch CategorySearch { get; set; }
    public Cache Cache { get; set; }
}

public class EhamNet
{
    public string Title { get; set; }
    public KeywordSearch KeywordSearch { get; set; }
    public CategorySearch CategorySearch { get; set; }
    public Cache Cache { get; set; }
}

public class AppSettings
{
    public EmailSettings Email { get; set; }
    /// <summary>
    /// Where to put assets (ex: shared web folder)
    /// </summary>
    public string ResourceFolder { get; set; }
    /// <summary>
    /// Url to access cached assets via http
    /// </summary>
    public string ResourceUrl { get; set; }
    public QthCom QthCom { get; set; }
    public EhamNet EhamNet { get; set; }
}
