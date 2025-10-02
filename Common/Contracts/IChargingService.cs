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
        [FaultContract(typeof(FaultException))]
        void StartSession(string vehicleId);

        [OperationContract]
        [FaultContract(typeof(FaultException))]
        void PushSample(ChargingSample sample);

        [OperationContract]
        [FaultContract(typeof(FaultException))]
        void EndSession(string vehicleId);
    }
}
