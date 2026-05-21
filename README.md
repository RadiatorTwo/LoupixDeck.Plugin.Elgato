# LoupixDeck.Plugin.Elgato

Elgato Key Lights integration plugin for [LoupixDeck](https://github.com/RadiatorTwo/LoupixDeck),
built against [LoupixDeck.PluginSdk](https://github.com/RadiatorTwo/LoupixDeck.PluginSdk).

## Commands

Toggle, set/change brightness, temperature, hue and saturation
(`System.ElgKl*`) — offered per discovered Key Light in the command menu.

## Settings

Key Lights are discovered automatically via mDNS/Zeroconf. The plugin settings
page offers a "Rescan for Key Lights" action; discovered lights are stored in
`plugins/elgato/settings.json`.

## Build & deploy

```bash
dotnet build LoupixDeck.Plugin.Elgato.csproj -c Release
```

Copy the build output together with `plugin.json` into
`LoupixDeck/plugins/elgato/`.
