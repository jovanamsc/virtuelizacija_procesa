using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace Common.Contracts
{
    [DataContract]
    public class ChargingSample
    {
        [DataMember]
        public int RowIndex { get; set; }

        [DataMember]
        public string VehicleId { get; set; }

        [DataMember]
        public DateTime Timestamp { get; set; }

        // Voltage RMS
        [DataMember] public double VoltageRmsMin { get; set; }
        [DataMember] public double VoltageRmsAvg { get; set; }
        [DataMember] public double VoltageRmsMax { get; set; }

        // Current RMS
        [DataMember] public double CurrentRmsMin { get; set; }
        [DataMember] public double CurrentRmsAvg { get; set; }
        [DataMember] public double CurrentRmsMax { get; set; }

        // Real / Reactive / Apparent Power
        [DataMember] public double RealPowerMin { get; set; }
        [DataMember] public double RealPowerAvg { get; set; }
        [DataMember] public double RealPowerMax { get; set; }

        [DataMember] public double ReactivePowerMin { get; set; }
        [DataMember] public double ReactivePowerAvg { get; set; }
        [DataMember] public double ReactivePowerMax { get; set; }

        [DataMember] public double ApparentPowerMin { get; set; }
        [DataMember] public double ApparentPowerAvg { get; set; }
        [DataMember] public double ApparentPowerMax { get; set; }

        // Frequency
        [DataMember] public double FrequencyMin { get; set; }
        [DataMember] public double FrequencyAvg { get; set; }
        [DataMember] public double FrequencyMax { get; set; }
    }
}
