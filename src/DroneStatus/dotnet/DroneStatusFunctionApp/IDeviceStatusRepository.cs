using Microsoft.Azure.Documents;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace DroneStatusFunctionApp
{
    public interface IDeviceStatusRepository
    {
        Task<Document> GetStatusDocumentAsync(string deviceId, ILogger log);
    }
}