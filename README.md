# CrowdMute

Tired of annoying crowd noises and voices? CrowdMute mutes the **Ambient Sounds** channel when too many players are nearby, and restores your volume when the crowd thins out.

## Usage

- Open the settings window via the Dalamud plugin menu.
- Set **Mute at least** to the number of nearby players where you want ambient sound to mute.
- **Unmute margin** adds hysteresis to prevent flapping (e.g., mute at 10, unmute when below 5 with margin 5).
- **Radius** limits the count to players within that many yalms (default 50).
- **Unmute delay** is how long the area must stay clear before volume is restored.

## Installation

Add the custom repo URL in your Dalamud plugin installer:

```
https://raw.githubusercontent.com/NeonGhostCinnamon/CrowdMute/master/repo.json
```
