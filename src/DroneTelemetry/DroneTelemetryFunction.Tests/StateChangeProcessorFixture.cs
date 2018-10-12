using DroneTelemetryFunctionApp;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace DroneTelemetryFunction.Tests
{
    [TestClass]
    public class StateChangeProcessorFixture
    {
        private Mock<IDocumentClient> CreateMockClient(Document readDocument)
        {
            var client = new Mock<IDocumentClient>();

            client.Setup(c => c.ReadDocumentAsync(It.IsAny<Uri>(), It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Uri u, RequestOptions r, CancellationToken t) => GetResourceResponse(readDocument));

            client.Setup(c => c.UpsertDocumentAsync(It.IsAny<Uri>(), It.IsAny<object>(), It.IsAny<RequestOptions>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Uri u, object o, RequestOptions r, bool b, CancellationToken t) => new ResourceResponse<Document>(GetDocumentFromObject(o)));

            return client;
        }

        private ResourceResponse<Document> GetResourceResponse(Document readDocument)
        {
            if (readDocument == null) ThrowDocumentClientNotFoundException();
            return new ResourceResponse<Document>(readDocument);
        }

        private static void ThrowDocumentClientNotFoundException()
        {
            var type = typeof(DocumentClientException);

            var documentClientExceptionInstance = type.Assembly.CreateInstance(type.FullName,
                false, BindingFlags.Instance | BindingFlags.NonPublic, null,
                new object[] { new Error(), null, HttpStatusCode.NotFound }, null, null);

            var ex = (DocumentClientException)documentClientExceptionInstance;
            throw ex;
        }

        private Document GetDocumentFromObject(object o)
        {
            var deviceState = (DeviceState)o;
            var document = new Document();
            document.SetPropertyValue("id", deviceState.DeviceId);
            if (deviceState.Battery != null) document.SetPropertyValue("Battery", deviceState.Battery);
            if (deviceState.FlightMode != null) document.SetPropertyValue("FlightMode", deviceState.FlightMode);
            if (deviceState.Latitude != null) document.SetPropertyValue("Latitude", deviceState.Latitude);
            if (deviceState.Longitude != null) document.SetPropertyValue("Longitude", deviceState.Longitude);
            if (deviceState.Altitude != null) document.SetPropertyValue("Altitude", deviceState.Altitude);
            if (deviceState.AccelerometerOK != null) document.SetPropertyValue("AccelerometerOK", deviceState.AccelerometerOK);
            if (deviceState.GyrometerOK != null) document.SetPropertyValue("GyrometerOK", deviceState.GyrometerOK);
            if (deviceState.MagnetometerOK != null) document.SetPropertyValue("MagnetometerOK", deviceState.MagnetometerOK);

            return document;
        }

        [TestMethod]
        public async Task StateChangeProcessor_CreateDocument()
        {
            var client = CreateMockClient(null);

            var processor = new StateChangeProcessor(client.Object, "db", "col");
            var logger = new Mock<ILogger>();

            var updatedState = new DeviceState();
            updatedState.DeviceId = "device001";
            updatedState.Battery = 1;

            var result = await processor.UpdateState(updatedState, logger.Object);
            DeviceState resultDoc = (dynamic)result.Resource;

            Assert.AreEqual("device001", resultDoc.DeviceId);
            Assert.AreEqual(1, resultDoc.Battery);
        }

        [TestMethod]
        public async Task StateChangeProcessor_MergeDocument()
        {
            var doc1 = new Document() { Id = "device001" };
            doc1.SetPropertyValue("Battery", 0.5);
            var client = CreateMockClient(doc1);

            var processor = new StateChangeProcessor(client.Object, "db", "col");
            var logger = new Mock<ILogger>();

            var update = new DeviceState();
            update.DeviceId = "device001";
            update.Latitude = 10;
            update.Longitude = 20;
            update.Altitude = 30;

            var response = await processor.UpdateState(update, logger.Object);
            DeviceState result = (dynamic)response.Resource;

            Assert.AreEqual("device001", result.DeviceId);
            Assert.AreEqual(0.5, result.Battery);
            Assert.AreEqual(10, result.Latitude);
            Assert.AreEqual(20, result.Longitude);
            Assert.AreEqual(30, result.Altitude);
        }


    }
}
