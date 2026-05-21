using Newtonsoft.Json;

namespace LoupixDeck.Plugin.Elgato;

/// <summary>A discovered Elgato Key Light and its last-known state.</summary>
public sealed class KeyLight
{
    public string DisplayName { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Address { get; set; } = string.Empty;

    public KeyLight()
    {
    }

    public KeyLight(string displayName, int port, string address)
    {
        DisplayName = displayName;
        Port = port;
        Address = address;
    }

    [JsonIgnore]
    public string Url => $"http://{Address}:{Port}/elgato/lights";

    public bool On { get; set; }
    public int Brightness { get; set; }
    public int Temperature { get; set; }
    public int Hue { get; set; }
    public int Saturation { get; set; }
}
