# ReaParamView Plugin

A REAPER DAW plugin that monitors active track envelopes and broadcasts their values in real-time to a connected web application via UDP.

## Overview

This plugin monitors the envelopes on your currently selected track in REAPER and sends the values to a web application in real-time. You can then view and monitor these envelope values in your browser.

### Key Features

- **Real-time Monitoring**: Sends envelope values to the web app (updates every 250ms by default)
- **Configurable**: Choose where to send data and how often to update
- **8 Parameters**: Display up to 8 envelope values at once
- **Easy Setup**: Organize envelopes by name if you want to control which ones display

## Installation

1. Build the plugin project:
   ```powershell
   dotnet build -c Release
   ```

2. The compiled plugin (as a native DLL) needs to be placed in REAPER's plugins directory:
   - Windows: `C:\Users\[UserName]\AppData\Roaming\REAPER\UserPlugins`
   - The plugin file should be named according to REAPER's naming convention

3. Restart REAPER to load the plugin

## Configuration

### Settings File

The plugin looks for a configuration file at:
```
~\reaparamview.json
```

Create this file in your user profile directory to customize monitoring and UDP transport settings:

```json
{
  "MonitorSettings": {
    "Host": "127.0.0.1",
    "Port": 9000,
    "UpdateIntervalMs": 250
  }
}
```

### Configuration Options

| Setting | Default | Description |
|---------|---------|-------------|
| `Host` | `127.0.0.1` | Where to send the envelope data |
| `Port` | `9000` | What port to send it to |
| `UpdateIntervalMs` | `250` | How often to send updates (in milliseconds) |

### Examples

**Send data to a remote host:**
```json
{
  "MonitorSettings": {
    "Host": "192.168.1.100",
    "Port": 9000,
    "UpdateIntervalMs": 250
  }
}
```

**Increase update frequency (send every 100ms):**
```json
{
  "MonitorSettings": {
    "Host": "127.0.0.1",
    "Port": 9000,
    "UpdateIntervalMs": 100
  }
}
```

### Logging Configuration

By default, the plugin logs only `Error` level events to minimize output. You can customize logging behavior by adding a `Logging` section to your configuration file.

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `Logging.LogLevel.Default` | string | `Error` | Global default log level |
| `Logging.LogLevel.[Logger]` | string | inherited | Per-logger log level (e.g., `ReaParamView.Plugin`) |

**Available log levels:** `Trace`, `Debug`, `Information`, `Warning`, `Error`, `Critical`, `None`

**Enable debug logging for all components:**
```json
{
  "MonitorSettings": {
    "Host": "127.0.0.1",
    "Port": 9000,
    "UpdateIntervalMs": 250
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}
```

**Enable debug logging only for ActiveEnvelopeMonitor:**
```json
{
  "MonitorSettings": {
    "Host": "127.0.0.1",
    "Port": 9000,
    "UpdateIntervalMs": 250
  },
  "Logging": {
    "LogLevel": {
      "Default": "Error",
      "ReaParamView.Plugin.ActiveEnvelopeMonitor": "Debug"
    }
  }
}
```

**Enable information logging for all ReaParamView components:**
```json
{
  "MonitorSettings": {
    "Host": "127.0.0.1",
    "Port": 9000,
    "UpdateIntervalMs": 250
  },
  "Logging": {
    "LogLevel": {
      "Default": "Error",
      "ReaParamView": "Information"
    }
  }
}
```

If the configuration file is missing or incomplete, the plugin uses the default values defined in `MonitorSettings` and logs at `Error` level.

## Envelope Configuration

Envelopes on a track are automatically monitored and assigned to slots (1-8). You can control slot assignment through the envelope name:

### Automatic Slot Assignment

If no explicit slot is specified, envelopes are automatically assigned to available slots in alphabetical order by name.

**Example:**
- Track has envelopes: "Reverb", "Volume", "Pan"
- Result: 
  - Slot 1: "Pan" 
  - Slot 2: "Reverb"
  - Slot 3: "Volume"

### Explicit Slot Assignment

To assign an envelope to a specific slot, prefix the envelope name with `@N` (where N is 1-8):

```
@1 Volume
@2 Pan
@3 Reverb
@4 Dry/Wet
```

**Format Rules:**
- Use `@` followed by the slot number (1-8)
- Add a space after the slot number
- The remaining text is the display name
- If only `@N` is specified with no display name, the full original name is used

**Examples:**
| Envelope Name | Slot | Display Name |
|---|---|---|
| `@1 Master Volume` | 1 | Master Volume |
| `@2 Pan` | 2 | Pan |
| `@3` | 3 | (uses full original name) |
| `Volume` (no prefix) | Auto | Volume |

### Best Practices for Envelope Configuration

1. **Name Your Envelopes**: Use clear, descriptive names for all envelopes
2. **Use Explicit Slots when Order Matters**: If the visual order in the web app is important, use explicit `@N` prefixes
3. **Keep Names Short**: Display names in the web app are space-constrained
4. **Avoid Special Characters**: Stick to alphanumeric characters and spaces
5. **Turn Off Unused Envelopes**: Only active envelopes are monitored; disable envelopes you don't want to monitor

## Usage Workflow

1. **Launch REAPER** with the plugin installed
2. **Select a track** that has envelopes you want to monitor
3. **Configure envelopes** using the naming convention above
4. **Enable the envelopes** that should be monitored
5. **Configure the plugin** via `reaparamview.json` (optional; defaults are used if not configured)
6. **Start the web application** (ReaParamView.WebApp) listening on the configured UDP host and port
7. **Envelope data** will automatically stream to the web app at the configured update interval (default 250ms)

The plugin continuously monitors and updates as you:
- Select different tracks
- Adjust envelope values
- Enable/disable envelopes
- Change track selection