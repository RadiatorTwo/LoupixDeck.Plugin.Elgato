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
    private List<IPluginCommand> _commands = [];
    private IPluginHost? _host;

    public override PluginMetadata Metadata { get; } = new()
    {
        Id = "elgato",
        Name = "Elgato Key Lights",
        Version = new Version(1, 0, 0),
        SdkVersion = new Version(1, 1, 0),
        Author = "RadiatorTwo",
        Description = "Discover and control Elgato Key Lights (brightness, temperature, hue, saturation)."
    };

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

    private async void OnKeyLightFound(object? sender, KeyLight light)
    {
        try
        {
            var existing = _devices.KeyLights.FirstOrDefault(kl => kl.DisplayName == light.DisplayName);
            if (existing != null)
                _devices.RemoveKeyLight(existing);

            await _controller.InitDeviceAsync(light);
            _devices.AddKeyLight(light);
        }
        catch (Exception ex)
        {
            _host?.Logger.Warn($"Failed to initialize Key Light '{light.DisplayName}': {ex.Message}");
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
                await _controller.ProbeForElgatoDevices();
                return $"{_devices.KeyLights.Count} Key Light(s) known.";
            }
        }
    ];

    private IReadOnlyList<PluginSettingAction>? _settingsActions;

    public void OnSettingsSaved()
    {
    }
}
