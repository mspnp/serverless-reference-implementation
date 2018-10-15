namespace Serverless.Simulator
{
    using System;
    using Serverless.Serialization.Models;

    sealed public class TelemetryGenerator
    {

        private static readonly Random randomizer = new Random();

        // Battery control
        private const double MinimumBatteryLevel = 0.1;
        private const double MaximumBatteryLevel = 1.0;
        private const double BatteryVariation = 2;

        // Flight mode control
        private static int flightModeCycle = 1;

        // Position control
        private const double DefaultLatitude = 47.476075;
        private const double DefaultLongitude = -122.192026;

        // Altitude control
        private const double DefaultAltitude = 0.0;
        private const double AverageAltitude = 499.99;
        private const double AltitudeVariation = 5;

        // Store enum size once for reuse in code
        private static readonly int FlightModeSize = Enum.GetValues(typeof(DroneFlightMode)).Length;

        public static DroneState GetTimeElapsedTelemetry(DroneState previousState, string deviceId, bool keyFrame = false)
        {
            if (previousState == null)
            {
                return GetFirstFrame(deviceId);
            }

            var droneState = new DroneState()
            {
                DeviceId = deviceId,
                IsKeyFrame = keyFrame
            };

            // If keyframe, initialize additional properties
            if (keyFrame)
            {
                droneState.Battery = Math.Round(VaryCondition((double)previousState.Battery.Value, BatteryVariation, MinimumBatteryLevel, (double)previousState.Battery.Value), 2);
                droneState.FlightMode = (DroneFlightMode)flightModeCycle;
                droneState.Health = (randomizer.Next(100) < 50, randomizer.Next(100) > 50, randomizer.Next(100) < 50);

                // Between -1.5 and 1.5 miles around start location
                var distance = Math.Round(VaryCondition(0.05, 2500, -1.5, 1.5), 2);
                droneState.Position = VaryLocation(previousState.Position.Value.Latitude, previousState.Position.Value.Longitude, distance);

                if (++flightModeCycle > FlightModeSize) flightModeCycle = 0;
            }

            return droneState;
        }

        private static double VaryCondition(double avg, double percentage, double min, double max)
        {
            var someValue = avg * (1 + ((percentage / 100) * (2 * randomizer.NextDouble() - 1)));
            someValue = Math.Max(someValue, min);
            someValue = Math.Min(someValue, max);
            return someValue;
        }

        private static (double Latitude, double Longitude, double Altitude) VaryLocation(double latitude, double longitude, double distance)
        {
            // Convert to meters, use Earth radius, convert to radians
            var radians = (distance * 1609.344 / 6378137) * (180 / Math.PI);
            return (
                Math.Round((latitude + radians), 6),
                Math.Round((longitude + radians / Math.Cos(latitude * Math.PI / 180)), 6),
                VaryCondition(AverageAltitude, AltitudeVariation, AverageAltitude - AltitudeVariation, AverageAltitude + AltitudeVariation)
            );
        }

        private static DroneState GetFirstFrame(string deviceId)
        {
            return new DroneState()
            {
                DeviceId = deviceId,
                Battery = MaximumBatteryLevel,
                FlightMode = DroneFlightMode.Offline,
                Position = (DefaultLatitude, DefaultLongitude, DefaultAltitude),
                Health = (true, true, true),
                IsKeyFrame = true
            };
        }
    }
}