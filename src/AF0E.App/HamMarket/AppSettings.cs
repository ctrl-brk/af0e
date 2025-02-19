// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global
namespace HamMarket;

public class SmtpSettings
{
    public bool Enabled { get; set; }
    public string SmtpServer { get; set; } = null!;
    public int Port { get; set; }
    public string User { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class SendGridSettings
{
    public bool Enabled { get; set; }
    public string ApiKey { get; set; } = null!;
}

public class EmailSettings
{
    public SmtpSettings Smtp { get; set; } = null!;
    public SendGridSettings SendGrid { get; set; } = null!;
    public string From { get; set; } = null!;
    public string To { get; set; } = null!;
    public string SubjectResultsFormat { get; set; } = null!;
    public string SubjectEmptyFormat { get; set; } = null!;
    /// <summary>
    /// File name to save generated html for an email/external access
    /// </summary>
    public string BodyFileName { get; set; } = null!;
    /// <summary>
    /// Whether to attach email body also as a html file
    /// </summary>
    public bool AttachFile { get; set; }
}

public class KeywordSearch
{
    public string Keywords { get; set; } = null!;
    public int MaxPosts { get; set; }
    public string ResultFile { get; set; } = null!;
}

public class CategorySearch
{
    public string Categories { get; set; } = null!;
    public int MaxPosts { get; set; }
    public string ResultFile { get; set; } = null!;
}

public class Cache
{
    /// <summary>
    /// Will be combined with ResourceUrl in the email and with ResourceFolder for storage
    /// </summary>
    /// <value>Where to cache images</value>
    public string ImageFolder { get; set; } = null!;
}

public class QthCom
{
    public bool Enabled { get; set; }
    public string Title { get; set; } = null!;
    public KeywordSearch KeywordSearch { get; set; } = null!;
    public CategorySearch CategorySearch { get; set; } = null!;
    public Cache Cache { get; set; } = null!;
}

public class EhamNet
{
    public bool Enabled { get; set; }
    public string Title { get; set; } = null!;
    public KeywordSearch KeywordSearch { get; set; } = null!;
    public CategorySearch CategorySearch { get; set; } = null!;
    public Cache Cache { get; set; } = null!;
}

public class AppSettings
{
    public EmailSettings Email { get; set; } = null!;
    /// <summary>
    /// Where to put assets (ex: shared web folder)
    /// </summary>
    public string ResourceFolder { get; set; } = null!;
    /// <summary>
    /// Url to access cached assets via http
    /// </summary>
    public string ResourceUrl { get; set; } = null!;
    public QthCom QthCom { get; set; } = null!;
    public EhamNet EhamNet { get; set; } = null!;
}
