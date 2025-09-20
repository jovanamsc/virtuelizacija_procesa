using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace Common.Contracts
{
    [ServiceContract]
    public interface IChargingService
    {
        [OperationContract]
        void StartSession(string vehicleId);

        [OperationContract]
        void PushSample(ChargingSample sample);

        [OperationContract]
        void EndSession(string vehicleId);
    }
}
