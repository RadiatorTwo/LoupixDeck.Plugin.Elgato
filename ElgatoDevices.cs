namespace LoupixDeck.Plugin.Elgato;

/// <summary>
/// In-memory registry of discovered Key Lights. Persistence is handled by
/// <see cref="ElgatoPlugin"/> via the plugin settings store.
/// </summary>
public sealed class ElgatoDevices
{
    public event EventHandler<KeyLight>? KeyLightAdded;
    public event EventHandler<KeyLight>? KeyLightRemoved;

    public List<KeyLight> KeyLights { get; } = [];

    public void AddKeyLight(KeyLight keyLight)
    {
        KeyLights.Add(keyLight);
        KeyLightAdded?.Invoke(this, keyLight);
    }

    public void RemoveKeyLight(KeyLight keyLight)
    {
        KeyLights.Remove(keyLight);
        KeyLightRemoved?.Invoke(this, keyLight);
    }
}
