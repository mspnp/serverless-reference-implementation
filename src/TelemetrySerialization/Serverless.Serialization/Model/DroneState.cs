using System;
using ProtoBuf;

namespace Serverless.Serialization.Models {
    [ProtoContract]
    public class DroneState {
        [ProtoMember (1)]
        public string DeviceId { get; set; }

        [ProtoMember (2)]
        public double? Battery { get; set; }

        [ProtoMember (3)]
        public DroneFlightMode? FlightMode { get; set; }

        [ProtoMember (4)]
        public (double Latitude, double Longitude, double Altitude) ? Position { get; set; }

        [ProtoMember (5)]
        public (bool GyrometerOK, bool AccelerometerOK, bool MagnetometerOK) ? Health { get; set; }

        [ProtoMember (6)]
        public bool IsKeyFrame { get; set; }

        public override string ToString () {
            return $"DeviceId: {DeviceId}, Battery: {Battery}, FlightMode: {Enum.GetName(typeof(DroneFlightMode), FlightMode)}, Position: ({Position.Value.Latitude}, {Position.Value.Longitude}, {Position.Value.Altitude}), Gyrometer: {(Health.Value.GyrometerOK?"OK":"NotOK")}, Accelerometer: {(Health.Value.AccelerometerOK?"OK":"NotOK")}, Magnetometer: {(Health.Value.MagnetometerOK?"OK":"NotOK")}, Keyframe: {(IsKeyFrame?"Yes":"No")}";
        }
    }

    [ProtoContract]
    public enum DroneFlightMode {
        [ProtoMember (1)]
        Unknown = 0,

        [ProtoMember (2)]
        Ready = 1,

        [ProtoMember (3)]
        Takeoff = 2,

        [ProtoMember (4)]
        Inflight = 3,

        [ProtoMember (5)]
        Landing = 4,

        [ProtoMember (6)]
        Offline = 5
    }
}