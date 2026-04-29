using Microsoft.Extensions.Options;
using System.Net;

namespace RigCommander;

public sealed class RigCommanderSettingsValidator : IValidateOptions<RigCommanderSettings>
{
    public ValidateOptionsResult Validate(string? name, RigCommanderSettings? options)
    {
        if (options is null)
            return ValidateOptionsResult.Fail("RigCommander configuration is missing.");

        var errors = new List<string>();
        var profiles = options.Profiles;

        if (profiles.Count == 0)
            errors.Add("RigCommander:Profiles must contain at least one entry.");

        var duplicates = profiles
            .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Count > 0)
            errors.Add("RigCommander:Profiles contains duplicate names: " + string.Join(", ", duplicates));

        foreach (var profile in profiles)
        {
            if (string.IsNullOrWhiteSpace(profile.Name))
            {
                errors.Add("RigCommander:Profiles entries require a Name.");
                continue;
            }

            switch (profile.Kind)
            {
                case RadioProfileKind.Icom:
                    ValidateIcom(profile, errors);
                    break;
                case RadioProfileKind.Yaesu:
                    ValidateYaesu(profile, errors);
                    break;
                default:
                    errors.Add($"RigCommander:Profiles[{profile.Name}] has unsupported Kind '{profile.Kind}'.");
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(options.ListenPort))
            errors.Add($"RigCommander::ListenPort is required.");

        if (!int.TryParse(options.ListenPort, out var port) || port <= 0 || port > 65535)
            errors.Add("RigCommander:ListenPort must be a valid port number between 1 and 65535.");

        if (!string.IsNullOrWhiteSpace(options.ActiveProfile) && profiles.FirstOrDefault(p => p.Name.Equals(options.ActiveProfile, StringComparison.OrdinalIgnoreCase)) is null)
            errors.Add($"RigCommander:ActiveProfile '{options.ActiveProfile}' was not found in Profiles.");

        if (options.StatusDelayMs < 0)
            errors.Add("RigCommander:StatusDelayMs must be zero or greater.");

        if (options.AdifUdp.Port is <= 0 or > 65535)
            errors.Add("RigCommander:AdifUdp:Port must be a valid port number between 1 and 65535.");

        if (options.AdifUdp.JoinMulticastGroup)
        {
            if (string.IsNullOrWhiteSpace(options.AdifUdp.MulticastGroup))
            {
                errors.Add("RigCommander:AdifUdp:MulticastGroup is required when JoinMulticastGroup is enabled.");
            }
            else if (!IPAddress.TryParse(options.AdifUdp.MulticastGroup, out var multicastAddress) || !IsIpv4Multicast(multicastAddress))
            {
                errors.Add("RigCommander:AdifUdp:MulticastGroup must be a valid IPv4 multicast address (224.0.0.0 to 239.255.255.255).");
            }
        }

        if (options.AdifUdp is { AcceptWsjtxFormat: false, AcceptRawAdif: false })
            errors.Add("RigCommander:AdifUdp must accept at least one format (AcceptWsjtxFormat or AcceptRawAdif).");

        if (options.AdifUdp.Forwarding.Enabled)
        {
            var forwarding = options.AdifUdp.Forwarding;

            if (string.IsNullOrWhiteSpace(options.LogbookApiUrl))
            {
                errors.Add("RigCommander:LogbookApiUrl is required when forwarding is enabled.");
            }
            else if (!Uri.TryCreate(options.LogbookApiUrl, UriKind.Absolute, out _))
            {
                errors.Add("RigCommander:LogbookApiUrl must be an absolute URI.");
            }

            if (forwarding.TimeoutSeconds <= 0)
                errors.Add("RigCommander:AdifUdp:Forwarding:TimeoutSeconds must be greater than zero.");

            if (forwarding.QueueCapacity <= 0)
                errors.Add("RigCommander:AdifUdp:Forwarding:QueueCapacity must be greater than zero.");

            if (forwarding.MaxRetries < 0)
                errors.Add("RigCommander:AdifUdp:Forwarding:MaxRetries must be zero or greater.");

            if (forwarding.RetryDelayMs < 0)
                errors.Add("RigCommander:AdifUdp:Forwarding:RetryDelayMs must be zero or greater.");

            if (string.IsNullOrWhiteSpace(forwarding.ApiKeyHeaderName))
                errors.Add("RigCommander:AdifUdp:Forwarding:ApiKeyHeaderName is required when forwarding is enabled.");

            if (forwarding.SkipWhenProcessRunning.Any(string.IsNullOrWhiteSpace))
                errors.Add("RigCommander:AdifUdp:Forwarding:SkipWhenProcessRunning cannot contain blank process names.");
        }

        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }

    private static void ValidateIcom(RadioProfileSettings profile, List<string> errors)
    {
        if (profile.Icom is null)
        {
            errors.Add($"RigCommander:Profiles[{profile.Name}] Kind=Icom requires the Icom block.");
            return;
        }

        var icom = profile.Icom;
        if (string.IsNullOrWhiteSpace(icom.PortName))
            errors.Add($"RigCommander:Profiles[{profile.Name}].Icom.PortName is required.");

        if (icom.BaudRate <= 0)
            errors.Add($"RigCommander:Profiles[{profile.Name}].Icom.BaudRate must be greater than zero.");
    }

    private static void ValidateYaesu(RadioProfileSettings profile, List<string> errors)
    {
        if (profile.Yaesu is null)
        {
            errors.Add($"RigCommander:Profiles[{profile.Name}] Kind=Yaesu requires the Yaesu block.");
            return;
        }

        var yaesu = profile.Yaesu;
        if (string.IsNullOrWhiteSpace(yaesu.PortName))
            errors.Add($"RigCommander:Profiles[{profile.Name}].Yaesu.PortName is required.");

        if (yaesu.BaudRate <= 0)
            errors.Add($"RigCommander:Profiles[{profile.Name}].Yaesu.BaudRate must be greater than zero.");

        if (yaesu.ReplyDelayMs < 0)
            errors.Add($"RigCommander:Profiles[{profile.Name}].Yaesu.ReplyDelayMs must be zero or greater.");

        if (yaesu.ReadTimeoutMs <= 0)
            errors.Add($"RigCommander:Profiles[{profile.Name}].Yaesu.ReadTimeoutMs must be greater than zero.");
    }

    private static bool IsIpv4Multicast(IPAddress address)
    {
        if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            return false;

        var firstOctet = address.GetAddressBytes()[0];
        return firstOctet is >= 224 and <= 239;
    }
}
