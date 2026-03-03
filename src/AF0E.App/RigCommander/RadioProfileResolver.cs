namespace RigCommander;

public static class RadioProfileResolver
{
    public static RadioProfileSettings Resolve(RigCommanderSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var profiles = settings.Profiles;
        if (profiles.Count == 0)
            throw new InvalidOperationException("No radio profiles have been configured.");

        if (string.IsNullOrWhiteSpace(settings.ActiveProfile))
            return profiles[0];

        var profile = settings.FindProfileByName(settings.ActiveProfile.Trim());
        return profile ?? profiles[0];
    }
}
