using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Xml;
using N1MMLookup.Properties;
using Timer = System.Windows.Forms.Timer;

namespace N1MMLookup;

public partial class AppForm : Form
{
    private const string Agent = "af0e_lookup";
    private Thread? _listener;
#pragma warning disable CA2213
    private UdpClient? _udpClient;
#pragma warning restore CA2213
    private ImgForm? _imgForm;
    private readonly object _lockObj = new();
    private static CancellationTokenSource? _cts;
    private static HttpClient _httpClient = null!;
    private static string? _sessionKey;
    private static string? _lastError;
    private static readonly Timer _configTimer = new() { Interval = 5000 };
    private static readonly Timer _imgTimer = new() { Interval = 500 };
    private static bool _hovered;
    private static bool _configChanged;
    private static bool _configLoaded;
    private static string? _imgUrl;

    public AppForm()
    {
        InitializeComponent();
    }

    private void AppForm_Load(object sender, EventArgs e)
    {
        LoadConfig();

        if (string.IsNullOrEmpty(Program.Settings.QrzApiUrl))
        {
            SetLastError(Resource.QrzApiUrlEmpty);
            return;
        }

        _configTimer.Tick += ConfigTimer_Tick;
        _imgTimer.Tick += ImgTimer_Tick;
        _configTimer.Start();

        var progress = new Progress<string>();

        progress.ProgressChanged += Progress_ProgressChanged;

        _httpClient = new HttpClient { BaseAddress = new Uri(Program.Settings.QrzApiUrl) };

        _listener = new Thread(() => ListenForData(progress));
        _listener.Start();
    }

    private void AppForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        _configTimer.Stop();
        _configTimer.Dispose();
        _imgTimer.Dispose();

        _imgForm?.CloseImage();
        _imgForm?.Close();

        _cts?.Cancel();

        _udpClient?.Close();
        _httpClient.Dispose();
        _udpClient?.Dispose();
        SaveConfig();
    }

    private void ListenForData(IProgress<string> progress)
    {
        _udpClient = new UdpClient(Program.Settings.UdpPort);
        var remoteEndPoint = new IPEndPoint(IPAddress.Any, Program.Settings.UdpPort);

        try
        {
            while (true) //this will exit because of exception when we close the udp client from FormClosing
            {
                var receivedBytes = _udpClient.Receive(ref remoteEndPoint);
                var receivedText = System.Text.Encoding.UTF8.GetString(receivedBytes);

                progress.Report(receivedText);
                // Invoke method is used to safely interact with the UI from another thread
                /*
                Invoke((MethodInvoker)(() =>
                {
                    textBox1.AppendText(receivedText + "\n");
                }));
                */
            }
        }
        catch (SocketException e)
        {
            if (!e.Message.Contains("WSACancelBlockingCall")) // there will be an exception when we close the udp client outside of this function
                SetLastError(e);
        }
        catch (Exception e)
        {
            SetLastError(e);
        }
    }

    private async void Progress_ProgressChanged(object? sender, string xml)
    {
        var startIndex = xml.IndexOf("<call>", StringComparison.Ordinal) + 6;
        var endIndex = xml.IndexOf('<', startIndex);
        var callSign = xml[startIndex..endIndex];

        lock (_lockObj)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
        }

        await QueryQrz(callSign, _cts.Token);
    }

    private async Task QueryQrz(string callSign, CancellationToken ct)
    {
        var retry = false;

    RETRY_SESSION:
        await GetSessionKey(ct);

        if (string.IsNullOrEmpty(_sessionKey))
            return;

        var response = await _httpClient.GetAsync($"?s={_sessionKey};callsign={callSign};agent={Agent}", ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        var xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(body);
        var node = xmlDoc.SelectSingleNode("/*[local-name()='QRZDatabase']/*[local-name()='Session']");
        if (node is null)
        {
            SetLastError($"Invalid session XML:\n{body}");
            return;
        }

        var error = node["Error"]?.InnerText;
        if (error != null)
        {
            if (string.Equals(error, "Invalid session key", StringComparison.OrdinalIgnoreCase) && !retry)
            {
                _sessionKey = null;
                retry = true;
                goto RETRY_SESSION; // :))
            }

            if (error.StartsWith("Not found", StringComparison.OrdinalIgnoreCase))
            {
                ClearFields();
                lblCall.Text = Resource.NotFoundMsg;
                return;
            }

            var msg = node["Message"]?.InnerText;
            SetLastError($"{error}\n{msg}");
            return;
        }

        node = xmlDoc.SelectSingleNode("/*[local-name()='QRZDatabase']/*[local-name()='Callsign']");
        if (node is null)
        {
            SetLastError($"Invalid callsign XML:\n{body}");
            return;
        }

        ClearFields();
        // ReSharper disable LocalizableElement
        lblCall.Text = node["call"]?.InnerText.Replace('0', 'Ø');
        lblName.Text = node["name_fmt"]?.InnerText;
        if (node["class"]?.InnerText is not null)
            lblName.Text += $", {node["class"]?.InnerText}";

        lblAddr.Text = node["addr1"]?.InnerText;
        lblCity.Text = $"{node["addr2"]?.InnerText},{node["state"]?.InnerText},{node["zip"]?.InnerText}";
        lblCountry.Text = node["country"]?.InnerText;
        lnkQrz.Text = $"https://www.qrz.com/db/{node["call"]?.InnerText}";
        lnkQrz.Enabled = true;
        _imgUrl = node["image"]?.InnerText;
        // ReSharper enable LocalizableElement

        try
        {
            if (picBox.Visible && _imgUrl is not null)
                picBox.LoadAsync(_imgUrl);
            else
                picBox.ImageLocation = null;
        }
        catch {/*ignore*/}
    }

    private async Task GetSessionKey(CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(_sessionKey))
            return;

        if (string.IsNullOrEmpty(Program.Settings.QrzUser) || string.IsNullOrEmpty(Program.Settings.QrzPassword))
        {
            SetLastError(Resource.UserPasswordEmpty);
            return;
        }

        var response = await _httpClient.GetAsync($"?username={Program.Settings.QrzUser};password={Program.Settings.QrzPassword};agent={Agent}", ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        var xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(body);
        var node = xmlDoc.SelectSingleNode("/*[local-name()='QRZDatabase']/*[local-name()='Session']");
        if (node is null)
        {
            _sessionKey = null;
            SetLastError($"Invalid session XML:\n{body}");
            return;
        }

        var key = node["Key"]?.InnerText;
        if (!string.IsNullOrEmpty(key))
        {
            _sessionKey = key;
            return;
        }

        var error = node["Error"]?.InnerText;
        var msg = node["Message"]?.InnerText;
        SetLastError($"{error}\n{msg}");
        _sessionKey = null;
    }

    private void ClearFields()
    {
        lblCall.Text = lblName.Text = lblAddr.Text = lblCity.Text = lblCountry.Text = lnkQrz.Text = null;
        lnkQrz.Enabled = false;
    }

    private void SaveConfig()
    {
        try
        {
            var config = new Config { Location = Location, Size = Size };

            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AF0E-Lookup");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            path = Path.Combine(path, "settings.json");

            var jsonConfig = JsonSerializer.Serialize(config);

            File.WriteAllText(path, jsonConfig);
        }
        catch (Exception e)
        {
            SetLastError(e);
        }
    }

    private void LoadConfig()
    {
        try
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AF0E-Lookup",
                "settings.json");
            if (!File.Exists(path))
                return;

            var jsonConfig = File.ReadAllText(path);
            var config = JsonSerializer.Deserialize<Config>(jsonConfig);

            if (!config!.IsValid())
                return;

            Location = config.Location;
            Size = config.Size;
            picBox.Visible = Size.Width >= 345;
        }
        catch (Exception e)
        {
            SetLastError(e);
        }
        finally
        {
            _configLoaded = true;
        }
    }

    private void btnError_Click(object sender, EventArgs e)
    {
        MessageBox.Show(_lastError, Resource.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
        ClearLastError();
    }

    private void SetLastError(Exception e)
    {
        SetLastError($"{e.Message}\n{e.InnerException?.Message}");
    }

    private void SetLastError(string msg)
    {
        _lastError = msg;
        btnError.Visible = true;
    }

    private void ClearLastError()
    {
        _lastError = null;
        btnError.Visible = false;
    }

    private void ConfigTimer_Tick(object? sender, EventArgs e)
    {
        if (!_configChanged)
            return;

        _configChanged = false;
        SaveConfig();
    }

    private void AppForm_SizeChanged(object sender, EventArgs e)
    {
        if (!_configLoaded)
            return;

        picBox.Visible = Size.Width >= 345;
        _configTimer.Enabled = false;
        _configChanged = true;
        _configTimer.Enabled = true;

    }

    private void AppForm_Move(object sender, EventArgs e)
    {
        if (!_configLoaded) return;

        _configTimer.Enabled = false;
        _configChanged = true;
        _configTimer.Enabled = true;
    }

    private void picBox_Click(object sender, EventArgs e)
    {
        OpenImage();
    }

    private void OpenImage()
    {
        if (picBox.ImageLocation is null) return;

        _imgForm ??= new ImgForm();

        _imgForm.LoadImage(this, picBox.ImageLocation, picBox.PreferredSize);
    }

    private void lnkQrz_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        Process.Start(new ProcessStartInfo { FileName = lnkQrz.Text, UseShellExecute = true });
    }

    private void picBox_VisibleChanged(object sender, EventArgs e)
    {
        if (!picBox.Visible) return;

        if (picBox.ImageLocation is null && _imgUrl is not null)
            picBox.LoadAsync(_imgUrl);
    }

    private void lnkQrz_MouseHover(object sender, EventArgs e)
    {
        if (picBox.Visible) return;

        _hovered = true;
        _imgTimer.Start();
    }

    private void ImgTimer_Tick(object? sender, EventArgs e)
    {
        if (!_hovered)
            return;

        OpenImage();
    }

    private void lnkQrz_MouseLeave(object sender, EventArgs e)
    {
        _hovered = false;
        _imgTimer.Stop();
    }
}
