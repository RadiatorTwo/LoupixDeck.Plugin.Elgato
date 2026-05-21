using System.Text;
using Newtonsoft.Json.Linq;
using Zeroconf;

namespace LoupixDeck.Plugin.Elgato;

/// <summary>
/// Discovers Elgato Key Lights via mDNS/Zeroconf and controls them over their
/// local HTTP API.
/// </summary>
public sealed class ElgatoController : IDisposable
{
    public event EventHandler<KeyLight>? KeyLightFound;
    public event EventHandler<string>? KeyLightDisconnected;

    private ZeroconfResolver.ResolverListener? _listener;

    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(2)
    };

    public async Task ProbeForElgatoDevices()
    {
        _listener?.Dispose();
        _listener = ZeroconfResolver.CreateListener("_elg._tcp.local.", 4000, 2, TimeSpan.FromMinutes(2));

        _listener.ServiceFound += (s, e) =>
        {
            var keyLight = new KeyLight(e.DisplayName, e.Services.Values.First().Port, e.IPAddress);
            KeyLightFound?.Invoke(s, keyLight);
        };

        _listener.ServiceLost += (s, e) => KeyLightDisconnected?.Invoke(s, e.DisplayName);

        // Give the Key Lights two minutes to announce themselves.
        await Task.Delay(TimeSpan.FromMinutes(2));

        _listener.Dispose();
        _listener = null;
    }

    public async Task<bool> InitDeviceAsync(KeyLight keyLight)
    {
        try
        {
            var response = await _httpClient.GetAsync(keyLight.Url);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            InitKeyLight(keyLight, JObject.Parse(responseContent));
            return true;
        }
        catch (TaskCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested)
        {
            throw new Exception("Request was canceled or timed out", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new Exception("An error occurred during the web request", ex);
        }
    }

    private static void InitKeyLight(KeyLight keyLight, JObject json)
    {
        var light = json["lights"]?.First;
        if (light == null)
            return;

        keyLight.On = light["on"] != null && (int)light["on"]! == 1;
        keyLight.Brightness = light["brightness"] != null ? (int)light["brightness"]! : 0;
        keyLight.Temperature = light["temperature"] != null ? (int)light["temperature"]! : 0;
        keyLight.Hue = light["hue"] != null ? (int)light["hue"]! : 0;
        keyLight.Saturation = light["saturation"] != null ? (int)light["saturation"]! : 0;
    }

    public Task Toggle(KeyLight keyLight) => SetState(keyLight, !keyLight.On);

    private async Task SetState(KeyLight keyLight, bool on)
    {
        if (keyLight.On == on)
            return;

        await SendPutRequestAsync(keyLight.Url, $"{{\"lights\":[{{\"on\":{Convert.ToInt32(on)}}}]}}");
        keyLight.On = on;
    }

    public async Task SetBrightness(KeyLight keyLight, int brightness)
    {
        if (keyLight.Brightness == brightness)
            return;

        await SendPutRequestAsync(keyLight.Url, $"{{\"lights\":[{{\"brightness\":{brightness}}}]}}");
        keyLight.Brightness = brightness;
    }

    public async Task SetTemperature(KeyLight keyLight, int temperature)
    {
        if (keyLight.Temperature == temperature)
            return;

        await SendPutRequestAsync(keyLight.Url, $"{{\"lights\":[{{\"temperature\":{temperature}}}]}}");
        keyLight.Temperature = temperature;
    }

    public async Task SetHue(KeyLight keyLight, int hue)
    {
        if (keyLight.Hue == hue)
            return;

        await SendPutRequestAsync(keyLight.Url, $"{{\"lights\":[{{\"hue\":{hue}}}]}}");
        keyLight.Hue = hue;
    }

    public async Task SetSaturation(KeyLight keyLight, int saturation)
    {
        if (keyLight.Saturation == saturation)
            return;

        await SendPutRequestAsync(keyLight.Url, $"{{\"lights\":[{{\"saturation\":{saturation}}}]}}");
        keyLight.Saturation = saturation;
    }

    private async Task SendPutRequestAsync(string url, string jsonData)
    {
        var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
        try
        {
            var response = await _httpClient.PutAsync(url, content);
            response.EnsureSuccessStatusCode();
            await response.Content.ReadAsStringAsync();
        }
        catch
        {
            // device may be offline — ignored
        }
    }

    public void Dispose()
    {
        _listener?.Dispose();
        _httpClient.Dispose();
    }
}
