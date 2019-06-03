using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace DroneTelemetryFunctionApp
{
    public interface IStateChangeProcessor
    {
        Task<ResourceResponse<Document>> UpdateState(DeviceState source, ILogger log);
    }
}

