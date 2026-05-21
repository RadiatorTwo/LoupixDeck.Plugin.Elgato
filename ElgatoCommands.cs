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
    /// are surfaced per Key Light through the dynamic menu, never as flat leaves.</summary>
    protected static CommandDescriptor Describe(string name, string display, params string[] extraParams)
    {
        var parameters = new List<CommandParameter> { new("KeyLightName", typeof(string)) };
        foreach (var p in extraParams)
            parameters.Add(new CommandParameter(p, typeof(int)));

        var template = extraParams.Length == 0
            ? "({KeyLightName})"
            : "({KeyLightName}," + string.Join(",", extraParams.Select(p => "{" + p + "}")) + ")";

        return new CommandDescriptor
        {
            CommandName = name,
            DisplayName = display,
            Group = "Elgato Keylights",
            ParameterTemplate = template,
            Parameters = parameters,
            HiddenFromMenu = true
        };
    }
}

internal sealed class ElgatoKeylightToggleCommand(ElgatoController elgato, ElgatoDevices devices)
    : ElgatoCommandBase(elgato, devices)
{
    public override CommandDescriptor Descriptor { get; } = Describe("System.ElgKlToggle", "Toggle Keylight");

    protected override Task Run(KeyLight keyLight, string[] parameters) => Elgato.Toggle(keyLight);
}

internal sealed class ElgatoKeylightTemperatureCommand(ElgatoController elgato, ElgatoDevices devices)
    : ElgatoCommandBase(elgato, devices)
{
    public override CommandDescriptor Descriptor { get; } =
        Describe("System.ElgKlTemperature", "Set Temperature", "Temperature");

    protected override Task Run(KeyLight keyLight, string[] parameters) =>
        Elgato.SetTemperature(keyLight, ParseInt(parameters, 1));
}

internal sealed class ElgatoKeylightBrightnessCommand(ElgatoController elgato, ElgatoDevices devices)
    : ElgatoCommandBase(elgato, devices)
{
    public override CommandDescriptor Descriptor { get; } =
        Describe("System.ElgKlBrightness", "Set Brightness", "Brightness");

    protected override Task Run(KeyLight keyLight, string[] parameters) =>
        Elgato.SetBrightness(keyLight, ParseInt(parameters, 1));
}

internal sealed class ElgatoKeylightSaturationCommand(ElgatoController elgato, ElgatoDevices devices)
    : ElgatoCommandBase(elgato, devices)
{
    public override CommandDescriptor Descriptor { get; } =
        Describe("System.ElgKlSaturation", "Set Saturation", "Saturation");

    protected override Task Run(KeyLight keyLight, string[] parameters) =>
        Elgato.SetSaturation(keyLight, ParseInt(parameters, 1));
}

internal sealed class ElgatoKeylightHueCommand(ElgatoController elgato, ElgatoDevices devices)
    : ElgatoCommandBase(elgato, devices)
{
    public override CommandDescriptor Descriptor { get; } = Describe("System.ElgKlHue", "Set Hue", "Hue");

    protected override Task Run(KeyLight keyLight, string[] parameters) =>
        Elgato.SetHue(keyLight, ParseInt(parameters, 1));
}

internal sealed class ElgatoKeylightChangeBrightnessCommand(ElgatoController elgato, ElgatoDevices devices)
    : ElgatoCommandBase(elgato, devices)
{
    public override CommandDescriptor Descriptor { get; } =
        Describe("System.ElgKlChangeBrightness", "Change Brightness", "Step");

    protected override Task Run(KeyLight keyLight, string[] parameters) =>
        Elgato.SetBrightness(keyLight, keyLight.Brightness + ParseInt(parameters, 1));
}

internal sealed class ElgatoKeylightChangeTemperatureCommand(ElgatoController elgato, ElgatoDevices devices)
    : ElgatoCommandBase(elgato, devices)
{
    public override CommandDescriptor Descriptor { get; } =
        Describe("System.ElgKlChangeTemperature", "Change Temperature", "Step");

    protected override Task Run(KeyLight keyLight, string[] parameters) =>
        Elgato.SetTemperature(keyLight, keyLight.Temperature + ParseInt(parameters, 1));
}

internal sealed class ElgatoKeylightChangeSaturationCommand(ElgatoController elgato, ElgatoDevices devices)
    : ElgatoCommandBase(elgato, devices)
{
    public override CommandDescriptor Descriptor { get; } =
        Describe("System.ElgKlChangeSaturation", "Change Saturation", "Step");

    protected override Task Run(KeyLight keyLight, string[] parameters) =>
        Elgato.SetSaturation(keyLight, keyLight.Saturation + ParseInt(parameters, 1));
}

internal sealed class ElgatoKeylightChangeHueCommand(ElgatoController elgato, ElgatoDevices devices)
    : ElgatoCommandBase(elgato, devices)
{
    public override CommandDescriptor Descriptor { get; } =
        Describe("System.ElgKlChangeHue", "Change Hue", "Step");

    protected override Task Run(KeyLight keyLight, string[] parameters) =>
        Elgato.SetHue(keyLight, keyLight.Hue + ParseInt(parameters, 1));
}
