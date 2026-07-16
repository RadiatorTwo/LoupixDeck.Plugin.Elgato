using System.Globalization;
using LoupixDeck.PluginSdk;

namespace LoupixDeck.Plugin.Elgato;

/// <summary>
/// Base for the Elgato commands. Resolves the target Key Light by display name
/// (the command's first parameter). Command names are kept identical to the
/// former built-in commands so existing button assignments keep working.
/// </summary>
internal abstract class ElgatoCommandBase(ElgatoController elgato, ElgatoDevices devices) : IPluginCommand
{
    public abstract CommandDescriptor Descriptor { get; }

    public ButtonTargets SupportedTargets => ButtonTargets.All;

    protected ElgatoController Elgato => elgato;

    public async Task Execute(CommandContext ctx)
    {
        if (ctx.Parameters.Length < 1)
        {
            Console.WriteLine($"{Descriptor.CommandName}: invalid parameter count");
            return;
        }

        var keyLight = devices.KeyLights.FirstOrDefault(kl => kl.DisplayName == ctx.Parameters[0]);
        if (keyLight == null)
            return;

        await Run(keyLight, ctx.Parameters);
    }

    protected abstract Task Run(KeyLight keyLight, string[] parameters);

    protected static int ParseInt(string[] parameters, int index)
    {
        if (parameters.Length <= index)
            return 0;

        return int.TryParse(parameters[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;
    }

    /// <summary>Builds a descriptor for an Elgato command. All Elgato commands
    /// are surfaced per Key Light through the dynamic menu, never as flat leaves.
    /// Each extra parameter is an int with an optional command-defined default value
    /// (SDK 1.17), which pre-fills the command's settings flyout on insert.</summary>
    protected static CommandDescriptor Describe(
        string name, string display, string icon, string description,
        params (string Name, string? Default)[] extraParams)
    {
        var parameters = new List<CommandParameter> { new("KeyLightName", typeof(string)) };
        foreach (var p in extraParams)
            parameters.Add(new CommandParameter(p.Name, typeof(int)) { DefaultValue = p.Default });

        var template = extraParams.Length == 0
            ? "({KeyLightName})"
            : "({KeyLightName}," + string.Join(",", extraParams.Select(p => "{" + p.Name + "}")) + ")";

        return new CommandDescriptor
        {
            CommandName = name,
            DisplayName = display,
            Group = "Elgato Keylights",
            Icon = icon,
            Description = description,
            ParameterTemplate = template,
            Parameters = parameters,
            HiddenFromMenu = true
        };
    }
}

internal sealed class ElgatoKeylightToggleCommand(ElgatoController elgato, ElgatoDevices devices)
    : ElgatoCommandBase(elgato, devices)
{
    public override CommandDescriptor Descriptor { get; } = Describe(
        "System.ElgKlToggle", "Toggle Keylight", "\U000F0335", "Toggle the key light on or off");

    protected override Task Run(KeyLight keyLight, string[] parameters) => Elgato.Toggle(keyLight);
}

internal sealed class ElgatoKeylightTemperatureCommand(ElgatoController elgato, ElgatoDevices devices)
    : ElgatoCommandBase(elgato, devices)
{
    public override CommandDescriptor Descriptor { get; } =
        Describe("System.ElgKlTemperature", "Set Temperature",
            "\U000F05A8", "Set the color temperature", ("Temperature", null));

    protected override Task Run(KeyLight keyLight, string[] parameters) =>
        Elgato.SetTemperature(keyLight, ParseInt(parameters, 1));
}

internal sealed class ElgatoKeylightBrightnessCommand(ElgatoController elgato, ElgatoDevices devices)
    : ElgatoCommandBase(elgato, devices)
{
    public override CommandDescriptor Descriptor { get; } =
        Describe("System.ElgKlBrightness", "Set Brightness",
            "\U000F00DF", "Set the brightness level", ("Brightness", "50"));

    protected override Task Run(KeyLight keyLight, string[] parameters) =>
        Elgato.SetBrightness(keyLight, ParseInt(parameters, 1));
}

internal sealed class ElgatoKeylightSaturationCommand(ElgatoController elgato, ElgatoDevices devices)
    : ElgatoCommandBase(elgato, devices)
{
    public override CommandDescriptor Descriptor { get; } =
        Describe("System.ElgKlSaturation", "Set Saturation",
            "\U000F062E", "Set the saturation value", ("Saturation", null));

    protected override Task Run(KeyLight keyLight, string[] parameters) =>
        Elgato.SetSaturation(keyLight, ParseInt(parameters, 1));
}

internal sealed class ElgatoKeylightHueCommand(ElgatoController elgato, ElgatoDevices devices)
    : ElgatoCommandBase(elgato, devices)
{
    public override CommandDescriptor Descriptor { get; } = Describe(
        "System.ElgKlHue", "Set Hue", "\U000F062E", "Set the hue value", ("Hue", null));

    protected override Task Run(KeyLight keyLight, string[] parameters) =>
        Elgato.SetHue(keyLight, ParseInt(parameters, 1));
}

internal sealed class ElgatoKeylightChangeBrightnessCommand(ElgatoController elgato, ElgatoDevices devices)
    : ElgatoCommandBase(elgato, devices)
{
    public override CommandDescriptor Descriptor { get; } =
        Describe("System.ElgKlChangeBrightness", "Change Brightness",
            "\U000F00DF", "Adjust brightness by a step", ("Step", "5"));

    protected override Task Run(KeyLight keyLight, string[] parameters) =>
        Elgato.SetBrightness(keyLight, keyLight.Brightness + ParseInt(parameters, 1));
}

internal sealed class ElgatoKeylightChangeTemperatureCommand(ElgatoController elgato, ElgatoDevices devices)
    : ElgatoCommandBase(elgato, devices)
{
    public override CommandDescriptor Descriptor { get; } =
        Describe("System.ElgKlChangeTemperature", "Change Temperature",
            "\U000F05A8", "Adjust color temperature by a step", ("Step", "10"));

    protected override Task Run(KeyLight keyLight, string[] parameters) =>
        Elgato.SetTemperature(keyLight, keyLight.Temperature + ParseInt(parameters, 1));
}

internal sealed class ElgatoKeylightChangeSaturationCommand(ElgatoController elgato, ElgatoDevices devices)
    : ElgatoCommandBase(elgato, devices)
{
    public override CommandDescriptor Descriptor { get; } =
        Describe("System.ElgKlChangeSaturation", "Change Saturation",
            "\U000F062E", "Adjust saturation by a step", ("Step", "5"));

    protected override Task Run(KeyLight keyLight, string[] parameters) =>
        Elgato.SetSaturation(keyLight, keyLight.Saturation + ParseInt(parameters, 1));
}

internal sealed class ElgatoKeylightChangeHueCommand(ElgatoController elgato, ElgatoDevices devices)
    : ElgatoCommandBase(elgato, devices)
{
    public override CommandDescriptor Descriptor { get; } =
        Describe("System.ElgKlChangeHue", "Change Hue",
            "\U000F062E", "Adjust hue by a step", ("Step", "10"));

    protected override Task Run(KeyLight keyLight, string[] parameters) =>
        Elgato.SetHue(keyLight, keyLight.Hue + ParseInt(parameters, 1));
}
