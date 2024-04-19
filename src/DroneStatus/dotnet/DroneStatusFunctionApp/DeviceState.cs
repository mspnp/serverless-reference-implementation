public class DeviceState
{
    public string id { get; set; }
    public double? Battery { get; set; }
    public int? FlightMode { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? Altitude { get; set; }
    public bool? GyrometerOK { get; set; }
    public bool? AccelerometerOK { get; set; }
    public bool? MagnetometerOK { get; set; }
}