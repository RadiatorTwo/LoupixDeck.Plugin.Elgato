using LoupixDeck.PluginSdk;

namespace LoupixDeck.Plugin.Elgato;

/// <summary>
/// Entry point of the Elgato Key Lights plugin. Discovers Key Lights on the
/// network, contributes per-light command submenus and persists the known
/// lights in the plugin settings store.
/// </summary>
public sealed class ElgatoPlugin : LoupixPlugin, IMenuContributor, IPluginSettingsPage
{
    private const string KeyLights = "keylights";

    private readonly ElgatoController _controller = new();
    private readonly ElgatoDevices _devices = new();
    private readonly SemaphoreSlim _registryGate = new(1, 1);
    private List<IPluginCommand> _commands = [];
    private IPluginHost? _host;

    public override PluginMetadata Metadata { get; } = new()
    {
        Id = "elgato",
        Name = "Elgato Key Lights",
        Version = new Version(1, 1, 0),
        SdkVersion = new Version(1, 17, 0),
        Author = "RadiatorTwo",
        Description = "Discover and control Elgato Key Lights (brightness, temperature, hue, saturation).",
        Icon = LoadIcon()
    };

    /// <summary>The plugin icon (icon.png, embedded). Missing data only costs the icon.</summary>
    private static byte[]? LoadIcon()
    {
        using Stream? stream = typeof(ElgatoPlugin).Assembly.GetManifestResourceStream("LoupixDeck.Plugin.Elgato.icon.png");
        if (stream == null) return null;

        using MemoryStream buffer = new();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    public override void Initialize(IPluginHost host)
    {
        _host = host;

        // Restore the previously known lights.
        var saved = host.Settings.Get<List<KeyLight>>(KeyLights, null);
        if (saved != null)
            _devices.KeyLights.AddRange(saved);

        _devices.KeyLightAdded += (_, _) => SaveKeyLights();
        _devices.KeyLightRemoved += (_, _) => SaveKeyLights();

        _controller.KeyLightFound += OnKeyLightFound;

        _commands =
        [
            new ElgatoKeylightToggleCommand(_controller, _devices),
            new ElgatoKeylightBrightnessCommand(_controller, _devices),
            new ElgatoKeylightTemperatureCommand(_controller, _devices),
            new ElgatoKeylightHueCommand(_controller, _devices),
            new ElgatoKeylightSaturationCommand(_controller, _devices),
            new ElgatoKeylightChangeBrightnessCommand(_controller, _devices),
            new ElgatoKeylightChangeTemperatureCommand(_controller, _devices),
            new ElgatoKeylightChangeSaturationCommand(_controller, _devices),
            new ElgatoKeylightChangeHueCommand(_controller, _devices)
        ];

        // Kick off a discovery probe in the background.
        _ = _controller.ProbeForElgatoDevices();
    }

    public override void Shutdown() => _controller.Dispose();

    public override IEnumerable<IPluginCommand> GetCommands() => _commands;

    public override IReadOnlyList<CommandGroupDescriptor> GetCommandGroups() =>
    [
        new CommandGroupDescriptor
        {
            Group = "Elgato Keylights",
            Description = "Key light control",
            Icon = "\U000F0335",
            Section = CommandGroupSection.Plugins
        }
    ];

    private async void OnKeyLightFound(object? sender, KeyLight light) => await RegisterKeyLight(light);

    /// <summary>
    /// Queries a discovered light and swaps it into the registry. Serialized because
    /// the background listener and the settings page's rescan both feed into it.
    /// </summary>
    private async Task RegisterKeyLight(KeyLight light)
    {
        // Query the light BEFORE touching the registry. Replacing the known entry
        // first meant a failing probe dropped the light from the list and — through
        // KeyLightRemoved -> SaveKeyLights — from the settings file as well, so a
        // single unreachable light erased it until the next successful discovery.
        try
        {
            await _controller.InitDeviceAsync(light);
        }
        catch (Exception ex)
        {
            _host?.Logger.Warn($"Failed to initialize Key Light '{light.DisplayName}': {ex.Message}");
            return;
        }

        await _registryGate.WaitAsync();
        try
        {
            var existing = _devices.KeyLights.FirstOrDefault(kl => kl.DisplayName == light.DisplayName);
            if (existing != null)
                _devices.RemoveKeyLight(existing);

            _devices.AddKeyLight(light);
        }
        finally
        {
            _registryGate.Release();
        }
    }

    private void SaveKeyLights()
    {
        if (_host == null)
            return;

        _host.Settings.Set(KeyLights, _devices.KeyLights);
        _host.Settings.Save();
    }

    // ───────── IMenuContributor — one submenu per Key Light ─────────

    public Task<IReadOnlyList<MenuNode>> GetMenuNodes(ButtonTargets target)
    {
        var keyLightNodes = new List<MenuNode>();

        foreach (var keyLight in _devices.KeyLights)
        {
            var commandLeaves = _commands.Select(c => new MenuNode
            {
                Name = c.Descriptor.DisplayName,
                CommandName = c.Descriptor.CommandName,
                // The Key Light name is the command's first parameter; the
                // host's command builder bakes it into the command string.
                Parameters = new Dictionary<string, string> { { "KeyLightName", keyLight.DisplayName } }
            }).ToList();

            keyLightNodes.Add(new MenuNode { Name = keyLight.DisplayName, Children = commandLeaves });
        }

        // With no known light the group would be empty; the host then shows it as a
        // bare card with no explanation. A single informational leaf (no command, no
        // children) states why instead — the host renders it as a non-actionable row.
        if (keyLightNodes.Count == 0)
            keyLightNodes.Add(new MenuNode
            {
                Name = "No Key Lights found — use 'Rescan for Key Lights' in Settings -> Plugins"
            });

        IReadOnlyList<MenuNode> result =
            [new MenuNode { Name = "Elgato Keylights", Children = keyLightNodes }];

        return Task.FromResult(result);
    }

    // ───────── IPluginSettingsPage ─────────

    public IReadOnlyList<PluginSettingDescriptor> SettingsSchema { get; } = [];

    public IReadOnlyList<PluginSettingAction> SettingsActions => _settingsActions ??=
    [
        new PluginSettingAction
        {
            Label = "Rescan for Key Lights",
            Invoke = async () =>
            {
                // A short, bounded scan. Awaiting ProbeForElgatoDevices instead would
                // hold the action for the listener's full two-minute lifetime.
                IReadOnlyList<KeyLight> found = await _controller.RescanAsync();
                foreach (KeyLight light in found)
                    await RegisterKeyLight(light);

                return $"{_devices.KeyLights.Count} Key Light(s) known.";
            }
        }
    ];

    private IReadOnlyList<PluginSettingAction>? _settingsActions;

    public void OnSettingsSaved()
    {
    }
}
