namespace DataSystem.Database.Dto;

public class SensorDataDto(SensorData result)
{
    public DateTime TimeStamp { get; set; } = result.TimeStamp;
    public string? DeviceId { get; set; } = result.DeviceId.ToString();
    public decimal? CarbonDioxide  { get; set; } = result.CarbonDioxide.HasValue ? Math.Round(result.CarbonDioxide.Value, 6) : null;
    public decimal? Humidity  { get; set; } = result.Humidity.HasValue ? Math.Round(result.Humidity.Value, 6) : null;
    public bool? Light  { get; set; } = result.Light;
    public decimal? Lpg  { get; set; } = result.Lpg.HasValue ? Math.Round(result.Lpg.Value, 6) : null;
    public bool? Motion  { get; set; } = result.Motion;
    public decimal? Smoke { get; set; } = result.Smoke.HasValue ? Math.Round(result.Smoke.Value, 6) : null;
    public decimal? Temperature  { get; set; } = result.Temperature;
    public string? AdditionalData  { get; set; } = result.AdditionalData;
}
