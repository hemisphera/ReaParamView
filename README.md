# ReaParamView

A real-time parameter visualization system for REAPER DAW consisting of a plugin and web application. Monitor track envelopes in REAPER and visualize them in your browser in real-time.

## Overview

ReaParamView consists of two main components:

1. **Plugin** - A REAPER extension that monitors track envelope values
2. **WebApp** - A web application that displays these values in real-time

Select a track in REAPER, and see the envelope values update live in your browser.

## Quick Start

### Prerequisites

- REAPER DAW installed
- .NET 10.0 or later
- A web browser

### Setup

#### 1. Build the Plugin

```powershell
cd ReaParamView.Plugin
dotnet build -c Release
```

Place the compiled DLL in REAPER's plugins folder.

#### 2. Build and Run the WebApp

```powershell
cd ReaParamView.WebApp
dotnet run
```

The app will start on `http://localhost:5000`

#### 3. Start REAPER

Load REAPER with the ReaParamView plugin. Select a track with active envelopes to see data in the web interface.

## Component Documentation

### [Plugin Documentation](ReaParamView.Plugin/README.md)

Complete documentation for the REAPER plugin including:
- Installation and setup
- Configuration options (Host, Port, Update Interval)
- Envelope configuration and slot assignment

### [WebApp Documentation](ReaParamView.WebApp/README.md)

Complete documentation for the web application including:
- Installation and setup
- Running the application
- Configuration options
- UI customization

## Configuration

Both components use JSON configuration files:

### Plugin Configuration

Location: `~/reaparamview.json`

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
      "ReaParamView.Plugin": "Debug"
    }
  }
}
```

### WebApp Configuration

Location: `ReaParamView.WebApp/appsettings.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Kestrel": {
    "Endpoints": {
      "http": {
        "Url": "http://*:5000"
      }
    }
  }
}
```

## Common Tasks

### Monitor Remote Track Parameters

1. Configure plugin to send to remote host:
   ```json
   {
     "MonitorSettings": {
       "Host": "192.168.1.100",
       "Port": 9000
     }
   }
   ```

2. Access the web app from the remote host on its configured port

### Enable Debug Logging

Update plugin configuration:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}
```

### Increase Update Frequency

Update plugin configuration to send every 100ms instead of default 250ms:
```json
{
  "MonitorSettings": {
    "UpdateIntervalMs": 100
  }
}
```

### Customize Parameter Display

Edit the UI component at `ReaParamView.WebApp/Components/Pages/Home.razor` to modify grid layout, colors, or information displayed.
