namespace ReaParamView.Plugin;

/// <summary>
/// Configuration settings for the monitor.
/// </summary>
public class MonitorSettings
{
    /// <summary>
    /// UDP host address where envelope data should be sent.
    /// Default: 127.0.0.1
    /// </summary>
    public string Host { get; set; } = "loalhost";

    /// <summary>
    /// UDP port where envelope data should be sent.
    /// Default: 9000
    /// </summary>
    public int Port { get; set; } = 9000;

    /// <summary>
    /// Update interval in milliseconds for sending envelope data.
    /// Default: 250
    /// </summary>
    public int UpdateIntervalMs { get; set; } = 250;
}