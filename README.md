# Crowd Mute

Dalamud plugin for FFXIV that mutes the **Ambient Sounds** channel when lots of players are nearby, and restores your volume when the crowd thins out.

## Usage

- `/crowdmute` opens the settings window.
- Set **Mute at this many players** a little below the count where the crowd noise starts. The window shows a live player count to help you tune it.
- Your original ambient volume is saved and restored when the area clears, when you disable the plugin, and on the next launch if the game closed while muted.

## Building

Requirements: XIVLauncher with Dalamud run at least once, and the .NET SDK.

```
dotnet build CrowdMute/CrowdMute.csproj -c Debug
```

Add the built `CrowdMute.dll` (in `CrowdMute/bin/...`) under `/xlsettings` > Experimental > Dev Plugin Locations, then enable it from `/xlplugins` > Dev Tools.

## AI disclosure

This plugin's code was written with the help of an AI assistant (Claude).
