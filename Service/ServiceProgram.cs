using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.ServiceModel;
using Common.Contracts;

namespace Service
{
    internal class ServiceProgram
    {
        static void Main(string[] args)
        {
            Uri baseAddress = new Uri("net.tcp://localhost:9000");

            NetTcpBinding binding = new NetTcpBinding
            {
                MaxReceivedMessageSize = 65536, 
                Security = { Mode = SecurityMode.None }
            };

            using (ServiceHost host = new ServiceHost(typeof(ChargingService), baseAddress))
            {
                host.AddServiceEndpoint(typeof(IChargingService), binding, "ChargingService");

                ChargingService.OnTransferStarted += (vehicleId) =>
                {
                    Console.WriteLine($"[EVENT] Prenos započet za vozilo {vehicleId}");
                };

                ChargingService.OnSampleReceived += (vehicleId, sample) =>
                {
                    Console.WriteLine($"[EVENT] Sample stigao za {vehicleId}, Row {sample.RowIndex}");
                };

                ChargingService.OnTransferCompleted += (vehicleId) =>
                {
                    Console.WriteLine($"[EVENT] Prenos završen za vozilo {vehicleId}");
                };

                ChargingService.OnWarningRaised += (vehicleId, warning) =>
                {
                    Console.WriteLine($"[WARNING] {warning} (vozilo {vehicleId})");
                };

                try
                {
                    host.Open();
                    Console.WriteLine("Servis je uspešno pokrenut na " + baseAddress);
                    Console.WriteLine("Pritisni bilo koji taster za zatvaranje...");
                    Console.ReadKey();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Greška prilikom pokretanja servisa: " + ex.Message);
                }
                finally
                {
                    if (host.State == CommunicationState.Faulted)
                        host.Abort();
                    else
                        host.Close();

                    Console.WriteLine("Servis zatvoren.");
                }
            };
        }
    }
}
