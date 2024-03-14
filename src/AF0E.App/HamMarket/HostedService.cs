﻿using System.Net.Mail;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using Attachment = System.Net.Mail.Attachment;

namespace HamMarket;

#pragma warning disable CA1001
public class HostedService : IHostedService
#pragma warning restore CA1001
{
    private readonly ILogger _logger;
    private readonly IHostApplicationLifetime _appLifeTime;
    private readonly AppSettings _settings;
    private readonly IQthHandler _qthHandler;
    private readonly IEhamHandler _ehamHandler;
    private readonly CookieContainer _cookies;

    private HttpClientHandler _httpClientHandler;
    private HttpClient _httpClient;

    private Task _task;
    private CancellationTokenSource _cts;

    public HostedService(ILogger<HostedService> logger, IHostApplicationLifetime appLifeTime, IOptions<AppSettings> settings, IQthHandler qthHandler, IEhamHandler ehamHandler)
    {
        _logger = logger;
        _appLifeTime = appLifeTime;
        _settings = settings.Value;
        _qthHandler = qthHandler;
        _ehamHandler = ehamHandler;
        _cookies = new CookieContainer();

        if (string.IsNullOrEmpty(_settings.ResourceFolder)) _settings.ResourceFolder = ".";
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogStarted();

        _httpClientHandler = new HttpClientHandler {UseCookies = false, CookieContainer = _cookies};
        _httpClient = new HttpClient(_httpClientHandler);

        // Create a linked token, so we can trigger cancellation outside of this token's cancellation
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _task = MonitorAsync(_cts.Token);

        // If the task is completed then return it, otherwise it's running
        return _task.IsCompleted ? _task : Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogStopping();
        _httpClient.Dispose();
        _httpClientHandler.Dispose();
        return Task.CompletedTask;
    }

    private async Task MonitorAsync(CancellationToken token)
    {
        var results = new List<ScanResult>();

        var keyRes = await _qthHandler.ProcessKeywordsAsync(_httpClient, null, token);
        if (keyRes != null)
            results.Add(keyRes);

        var catRes = await _qthHandler.ProcessCategoriesAsync(_httpClient, null, token);
        if (catRes != null)
            results.AddRange(catRes.Where(x => x != null));

        keyRes = await _ehamHandler.ProcessKeywordsAsync(_httpClient, _cookies, token);
        if (keyRes != null)
            results.Add(keyRes);

        catRes = await _ehamHandler.ProcessCategoriesAsync(_httpClient, _cookies, token);
        if (catRes != null)
            results.AddRange(catRes.Where(x => x != null));

        await SendResults(results);
        _appLifeTime.StopApplication();
    }

    private async Task SendResults(List<ScanResult> results)
    {
        var sb = new StringBuilder("""
                                   <!DOCTYPE html>
                                   <html lang='en'>
                                   <head>
                                   <meta charset='UTF-8'>
                                     <title>HamMarket search results</title>
                                     <style>
                                       * {box-sizing: border-box}
                                       html, body {margin:0; padding:0}
                                   
                                       .ext-link {text-align: right; width: 100%;}
                                       .ext-link a {color: #aaa;}
                                       .source {width: 100%; font-size: 2rem; font-weight: bold; text-align: center; color: cadetblue; }
                                       table {border: 1px solid #aaa; margin-bottom: 5px; width: 100%}
                                       tr, td {border: none; padding: 0; margin: 0}
                                       td.thumb {vertical-align: top; max-width: 300px}
                                       td.thumb img {width: 300px}
                                       td.thumb img.qth {max-width: 300px}
                                       td.thumb img.eham {max-width: 300px}
                                       td.title {height: 1.5rem; padding: 2px 5px; font: 1.2rem bold; font-family: helvetica; color: azure; background-color: cornflowerblue; width: 100%}
                                       td.title a.link {color: azure; text-decoration: none}
                                       td.title a.cat {float: right; font-size: 1rem; font-style: italic; color: oldlace}
                                       tr.content {height: 100%}
                                       tr.content td {padding: 10px 5px 0 5px; height: 100%; font-family: trebuchet ms; vertical-align: top}
                                       tr.content td .price {color: crimson}
                                       td.info {height: 1rem; padding: 10px 5px 0 5px; font-family: monospace; font-size: 0.8rem; vertical-align: bottom}
                                       td.info a.call {color: black;}
                                       td.info .modified {color: crimson}
                                   </style>
                                   </head>
                                   <body>

                                   """);

        if (!string.IsNullOrEmpty(_settings.Email.BodyFileName) && !string.IsNullOrEmpty(_settings.ResourceUrl))
            sb.AppendLine($"<div class='ext-link'><a href='{_settings.ResourceUrl}/{_settings.Email.BodyFileName}' target='_blank'>View this email in a separate browser window</a></div>");

        foreach(var res in results)
        {
            sb.AppendLine($"<div class='source'>{res.Title}</div>\n<div>");
            sb.AppendLine(res.Html);
            sb.AppendLine("</div>");
        }

        sb.AppendLine("</body>\n</html>");

        if (string.IsNullOrEmpty(_settings.Email.BodyFileName))
            return;

        _logger.LogSavingFile();
        await File.WriteAllTextAsync($"{_settings.ResourceFolder}/{_settings.Email.BodyFileName}", sb.ToString());

        if (_settings.Email.Smtp.Enabled)
        {
            _logger.LogSendingSmtpEmail("Smtp", _settings.Email.To);
        
            using var client = new SmtpClient(_settings.Email.Smtp.SmtpServer);
            
            using var msg = new MailMessage(_settings.Email.From, _settings.Email.To);

            msg.Subject = results.Count > 0 ?
                string.Format(_settings.Email.SubjectResultsFormat, results.Sum(x => x.Items), results.Min(x => x.LastScan)) :
                _settings.Email.SubjectEmptyFormat;
            msg.SubjectEncoding = Encoding.UTF8;
            msg.BodyEncoding = Encoding.UTF8;
            msg.IsBodyHtml = true;
            msg.Body = sb.ToString();

            if (!string.IsNullOrWhiteSpace(_settings.Email.Smtp.User))
                client.Credentials = new NetworkCredential(_settings.Email.Smtp.User, _settings.Email.Smtp.Password);

            if (_settings.Email.AttachFile && !string.IsNullOrEmpty(_settings.Email.BodyFileName))
                msg.Attachments.Add(new Attachment(_settings.Email.BodyFileName));

            try
            {
                client.Send(msg);
            }
            catch (Exception e)
            {
                _logger.LogException(e);
            }
        }

        if (_settings.Email.SendGrid.Enabled)
        {

            _logger.LogSendingSmtpEmail("Sendgrid", _settings.Email.To);
            
            var client = new SendGridClient(_settings.Email.SendGrid.ApiKey);
            
            var msg = new SendGridMessage
            {
                From = new EmailAddress(_settings.Email.From),
                Subject = results.Count > 0 ?
                    string.Format(_settings.Email.SubjectResultsFormat, results.Sum(x => x.Items), results.Min(x => x.LastScan)) :
                    _settings.Email.SubjectEmptyFormat,
                HtmlContent = sb.ToString(),
            };
            
            msg.AddTo(new EmailAddress(_settings.Email.To));

            if (_settings.Email.AttachFile && !string.IsNullOrEmpty(_settings.Email.BodyFileName))
            {
                var bytes = await File.ReadAllBytesAsync(_settings.Email.BodyFileName);
                msg.AddAttachment(_settings.Email.BodyFileName, Convert.ToBase64String(bytes));
            }

            try
            {
                var response = await client.SendEmailAsync(msg).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    _logger.LogSendEmailError(await response.Body.ReadAsStringAsync());
            }
            catch (Exception e)
            {
                _logger.LogException(e);
            }
        }
    }
}
