using DroneTelemetryFunctionApp;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Serverless.Serialization;
using Serverless.Serialization.Models;

namespace DroneTelemetryFunction.Tests
{
    [TestClass]
    public class TelemetryProcessorFixture
    {
        [TestMethod]
        public void TelemetryProcessor_KeyFrameCreatesDocument()
        {
            var droneState = new DroneState
            {
                DeviceId = "device001",
                Battery = 1,
                FlightMode = DroneFlightMode.Landing,
                Position = (10, 20, 30),
                Health = (true, false, true),
                IsKeyFrame = true
            };

            var serializer = new Mock<ITelemetrySerializer<DroneState>>();
            serializer.Setup(s => s.Deserialize(It.IsAny<byte[]>())).Returns(droneState);

            var processor = new TelemetryProcessor(serializer.Object);
            var logger = new Mock<ILogger>();

            var document = processor.Deserialize(new byte[0], logger.Object);

            Assert.AreEqual(document.DeviceId, droneState.DeviceId);
            Assert.AreEqual(document.Battery, droneState.Battery);
            Assert.AreEqual(document.FlightMode, (int)droneState.FlightMode);
            Assert.AreEqual(document.Latitude, droneState.Position.Value.Latitude);
            Assert.AreEqual(document.Longitude, droneState.Position.Value.Longitude);
            Assert.AreEqual(document.Altitude, droneState.Position.Value.Altitude);
            Assert.AreEqual(document.AccelerometerOK, droneState.Health.Value.AccelerometerOK);
            Assert.AreEqual(document.GyrometerOK, droneState.Health.Value.GyrometerOK);
            Assert.AreEqual(document.MagnetometerOK, droneState.Health.Value.MagnetometerOK);
        }

        [TestMethod]
        public void TelemetryProcessor_PartialStateCreatesDocument()
        {
            var droneState = new DroneState
            {
                DeviceId = "device001",
                IsKeyFrame = false
            };

            var serializer = new Mock<ITelemetrySerializer<DroneState>>();
            serializer.Setup(s => s.Deserialize(It.IsAny<byte[]>())).Returns(droneState);

            var processor = new TelemetryProcessor(serializer.Object);
            var logger = new Mock<ILogger>();

            var document = processor.Deserialize(new byte[0], logger.Object);

            Assert.AreEqual(document.DeviceId, droneState.DeviceId);

            Assert.IsNull(document.Battery);
            Assert.IsNull(document.FlightMode);
            Assert.IsNull(document.Latitude);
            Assert.IsNull(document.Longitude);
            Assert.IsNull(document.Altitude);
            Assert.IsNull(document.AccelerometerOK);
            Assert.IsNull(document.GyrometerOK);
            Assert.IsNull(document.MagnetometerOK);
        }
    }
}
