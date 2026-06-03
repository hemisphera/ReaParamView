namespace ReaParamView.Types;

public class ParameterSetDto
{
  public string? TrackName { get; set; }
  public ParameterDto[] Envelopes { get; set; } = Enumerable.Range(0, Constants.NoOfParameters).Select(_ => new ParameterDto()).ToArray();


  public void CopyFrom(ParameterSetDto other)
  {
    TrackName = other.TrackName;
    for (var i = 0; i < Envelopes.Length; i++)
    {
      Envelopes[i].Name = other.Envelopes[i].Name;
      Envelopes[i].Value = other.Envelopes[i].Value;
      Envelopes[i].FormattedValue = other.Envelopes[i].FormattedValue;
      Envelopes[i].Percentage = other.Envelopes[i].Percentage;
    }
  }
}