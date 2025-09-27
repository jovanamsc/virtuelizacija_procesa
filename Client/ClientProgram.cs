using Common.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    internal class ClientProgram
    {
        static void Main(string[] args)
        {
            // konfigurisanje wcf klijenta
            var binding = new NetTcpBinding
            {
                MaxReceivedMessageSize = 65536,
                Security = { Mode = SecurityMode.None }
            };
            var endpoint = new EndpointAddress("net.tcp://localhost:9000/ChargingService");

            var channelFactory = new ChannelFactory<IChargingService>(binding, endpoint);
            var proxy = channelFactory.CreateChannel();

            // baza
            string basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\EV-CPW Dataset");
            basePath = Path.GetFullPath(basePath);

            while (true)
            {
                Console.WriteLine("=== Odaberi vozilo ===");

                var vehicleFolders = Directory.GetDirectories(basePath);

                for (int i = 0; i < vehicleFolders.Length; i++)
                {
                    Console.WriteLine($"{i + 1}. {Path.GetFileName(vehicleFolders[i])}");
                }
                Console.WriteLine("0. Izlaz");

                Console.Write("Izbor: ");
                if (!int.TryParse(Console.ReadLine(), out int choice) || choice < 0 || choice > vehicleFolders.Length)
                {
                    Console.WriteLine("Neispravan unos. Pokušaj ponovo.\n");
                    continue;
                }

                if (choice == 0)
                    break;

                string selectedFolder = vehicleFolders[choice - 1];
                string vehicleId = Path.GetFileName(selectedFolder); 
                string csvPath = Path.Combine(selectedFolder, "Charging_Profile.csv");

                if (!File.Exists(csvPath))
                {
                    Console.WriteLine("CSV fajl nije pronađen.\n");
                    continue;
                }

                Console.WriteLine($"\nPokrećem sesiju za vozilo: {vehicleId}\n");

                try
                {
                    Console.WriteLine($"[DEBUG] Klijent šalje VehicleId: {vehicleId}");

                    proxy.StartSession(vehicleId);

                    var reader = new CsvReader(csvPath, vehicleId);

                    foreach (ChargingSample sample in reader.ReadRows())
                    {
                        try
                        {
                            proxy.PushSample(sample);
                            Console.WriteLine($"Row {sample.RowIndex} poslat.");
                        }
                        catch (FaultException ex)
                        {
                            Console.WriteLine($"Row {sample.RowIndex} odbijen: {ex.Message}");
                        }
                    }

                    proxy.EndSession(vehicleId);
                    Console.WriteLine("\nSesija završena.\n");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Greška u komunikaciji: {ex.Message}\n");
                }
            }
        }
    }
}
