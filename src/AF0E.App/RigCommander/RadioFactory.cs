using RigCommander.Abstractions;
using RigCommander.Radios.Icom;
using RigCommander.Radios.Yaesu;

namespace RigCommander;

public static class RadioFactory
{
    public static IRadio Create(RadioProfileSettings profile)
    {
        return profile.Kind switch
        {
            RadioProfileKind.Icom => CreateIcom(profile),
            RadioProfileKind.Yaesu => CreateYaesu(profile),
            _ => throw new InvalidOperationException($"Unsupported radio kind '{profile.Kind}'.")
        };
    }

#pragma warning disable CA1859
    private static IRadio CreateIcom(RadioProfileSettings profile)
    {
        if (profile.Icom is null)
            throw new InvalidOperationException($"Profile '{profile.Name}' is missing its Icom block.");

        var icom = profile.Icom;
        return profile.Name switch
        {
            "IC-7410" => new IC_7410(portName: icom.PortName, baudRate: icom.BaudRate, radioAddress: icom.RadioAddress, controllerAddress: icom.ControllerAddress),
            "IC-9100" => new IC_9100(portName: icom.PortName, baudRate: icom.BaudRate, radioAddress: icom.RadioAddress, controllerAddress: icom.ControllerAddress),
            _ => throw new InvalidOperationException($"Unsupported radio name '{profile.Name}'.")
        };
    }

    private static IRadio CreateYaesu(RadioProfileSettings profile)
    {
        if (profile.Yaesu is null)
            throw new InvalidOperationException($"Profile '{profile.Name}' is missing its Yaesu block.");

        var yaesu = profile.Yaesu;
        return new FT_897(
            portName: yaesu.PortName,
            baudRate: yaesu.BaudRate,
            dtrEnable: yaesu.DtrEnable,
            rtsEnable: yaesu.RtsEnable,
            replyDelayMs: yaesu.ReplyDelayMs,
            readTimeoutMs: yaesu.ReadTimeoutMs);
    }
}
