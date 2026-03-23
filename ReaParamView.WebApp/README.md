# ReaParamView WebApp

A real-time web application for visualizing REAPER track envelope parameters received from the ReaParamView plugin via UDP.

## Overview

This web application receives envelope data from the ReaParamView plugin and displays it in real-time. You'll see your track name and a grid of up to 8 envelope parameters with their current values and a visual representation.

### Key Features

- **Real-time Display**: Updates instantly as envelope values change
- **Grid Layout**: Shows up to 8 envelope parameters in an organized grid
- **Value Display**: Shows both the numeric value and percentage for each parameter
- **Visual Bars**: Progress bars for quick visual feedback
- **Track Name**: Displays which track you're currently monitoring

## Prerequisites

- .NET 10.0 or later
- ReaParamView Plugin running and configured
- Network connectivity between plugin host and web application host

## Installation & Setup

### 1. Build the WebApp

```powershell
cd ReaParamView.WebApp
dotnet build -c Release
```

### 2. Run the WebApp

```powershell
dotnet run
```

The application will start on `http://localhost:5000`

### 3. Connect the Plugin

Make sure the plugin is configured to send data to the WebApp. Update the plugin configuration file (`~/reaparamview.json`):

```json
{
  "MonitorSettings": {
    "Host": "127.0.0.1",
    "Port": 9000,
    "UpdateIntervalMs": 250
  }
}
```

## Usage

1. **Start the WebApp** (if not already running):
   ```powershell
   dotnet run
   ```

2. **Start REAPER** with the ReaParamView plugin installed

3. **Open your browser** and go to `http://localhost:5000`

4. **Select a track in REAPER** with active envelopes - the parameters will appear in the web app

5. **View your envelope values** as they update in real-time

## Configuration

The WebApp uses standard configuration via `appsettings.json`.

### Logging Configuration

Modify `appsettings.json` to control logging verbosity:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

**Log Levels:**
- `Trace`: Most detailed, rarely used
- `Debug`: Detailed diagnostic information
- `Information`: General informational messages (default)
- `Warning`: Warning messages for potential issues
- `Error`: Error messages
- `Critical`: Critical errors
- `None`: No logging

## Troubleshooting

### No Data Received

- **Check plugin is running**: Verify REAPER is running with the ReaParamView plugin installed
- **Check track selection**: Select a track with active envelopes in REAPER
- **Check network**: Verify network connectivity between plugin host and WebApp host
- **Check firewall**: Ensure UDP port 9000 (or configured port) is not blocked
- **Check configuration**: Verify plugin `MonitorSettings.Host` and `MonitorSettings.Port` match WebApp configuration
- **Check logs**: Review WebApp logs for UDP receiver errors

### Envelopes Not Showing

- **Enable envelopes**: Ensure envelopes are enabled (active) in REAPER
- **Configure envelope names**: Check envelope naming follows the `@N Name` format if explicit slot assignment is needed
- **Check slot limits**: Only 8 parameters are displayed; verify the track doesn't have more than 8 active envelopes

### WebApp Won't Start

- **Port in use**: Verify port 5000 (or configured port) is available
- **Build errors**: Ensure .NET 10.0 is installed: `dotnet --version`
- **Dependency issues**: Try restoring dependencies: `dotnet restore`

### Connection Timeouts

- **Firewall**: Check Windows Firewall settings for UDP port 9000
- **Network timeout**: Increase `UpdateIntervalMs` in plugin configuration if network is slow
- **Router**: Verify router allows UDP traffic between hosts



### Customizing the UI

To customize the appearance, edit the CSS file at `wwwroot/app.css`. The main page layout is in `Components/Pages/Home.razor`.

## License

See project LICENSE for details.
