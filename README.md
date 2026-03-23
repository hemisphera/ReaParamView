# ReaParamView Plugin

A REAPER DAW plugin that monitors active track envelopes and broadcasts their values in real-time to a connected web application via UDP.

## Overview

The ReaParamView Plugin runs as a REAPER extension and continuously monitors the envelopes on the currently selected track. It captures the envelope values (both raw and percentage) and sends them to a web application via UDP, enabling real-time visualization and monitoring of track parameters.

### Key Features

- **Real-time Monitoring**: Captures envelope values from the selected track (configurable update interval, default 250ms)
- **UDP Broadcasting**: Sends data to a configurable UDP host and port
- **8-Slot Support**: Supports up to 8 envelope parameters per track
- **Smart Slot Assignment**: Automatically assigns envelopes to available slots with support for explicit slot configuration
- **Track Awareness**: Includes track name in each message for context

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

The `MonitorSettings` class controls plugin behavior:

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `Host` | string | `localhost` | UDP host address where envelope data is sent |
| `Port` | int | `9000` | UDP port where envelope data is sent |
| `UpdateIntervalMs` | int | `250` | Update interval in milliseconds for sending envelope data |

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

## Message Format

The plugin sends binary-encoded messages containing track and envelope data:

### Binary Protocol

```
[Track Name Length: 1 byte][Track Name: N bytes]
[Envelope Count: 1 byte]
Per Envelope:
  [Slot: 1 byte]
  [Name Length: 1 byte][Name: N bytes]
  [Value: float32, 4 bytes]
  [Percentage: float32, 4 bytes]
```

**Data Types:**
- **Strings**: UTF-8 encoded, prefixed with 1-byte length
- **Numeric Values**: 32-bit floats (sufficient precision for audio parameters)
- **Slot**: 1-8 (byte value)

### Message Contents

Each message contains:
- **Track Name**: Name of the currently selected track
- **Envelope Count**: Number of active, monitored envelopes (max 8)
- **Per Envelope**:
  - Slot number (1-8)
  - Display name
  - Normalized value (typically 0.0-1.0 range depending on parameter)
  - Percentage value (0-100%)

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