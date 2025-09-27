using Common.Contracts;
using System;
using System.Globalization;
using System.IO;
using System.ServiceModel;

namespace Service
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession)]
    public class ChargingService : IChargingService
    {
        private string _currentVehicleId;
        private string _sessionFolder;
        private StreamWriter _writer;
        private StreamWriter _rejectWriter;

        public void StartSession(string vehicleId)
        {
            if (string.IsNullOrWhiteSpace(vehicleId))
                throw new FaultException("VehicleId ne sme biti prazan!");

            _currentVehicleId = vehicleId;

            Console.WriteLine($"[DEBUG SERVER] StartSession primljen za VehicleId: {vehicleId}");

            string date = DateTime.Now.ToString("yyyy-MM-dd");
            _sessionFolder = Path.Combine("Data", vehicleId, date);
            Directory.CreateDirectory(_sessionFolder);

            // session.csv
            string sessionPath = Path.Combine(_sessionFolder, "session.csv");
            _writer = new StreamWriter(new FileStream(sessionPath, FileMode.Append, FileAccess.Write));

            if (new FileInfo(sessionPath).Length == 0) // header samo ako je fajl prazan
                _writer.WriteLine("RowIndex,Timestamp,VoltageAvg,CurrentAvg,RealPowerAvg,FrequencyAvg");

            // rejects.csv
            string rejectPath = Path.Combine(_sessionFolder, "rejects.csv");
            _rejectWriter = new StreamWriter(new FileStream(rejectPath, FileMode.Append, FileAccess.Write));

            if (new FileInfo(rejectPath).Length == 0)
                _rejectWriter.WriteLine("RowIndex,Reason,RawData");

            Console.WriteLine($"[SERVER] StartSession za vozilo {vehicleId}");
        }

        public void PushSample(ChargingSample sample)
        {
            try
            {
                ValidateSample(sample);

                string line = $"{sample.RowIndex},{sample.Timestamp:O}," +
                              $"{sample.VoltageRmsAvg},{sample.CurrentRmsAvg}," +
                              $"{sample.RealPowerAvg},{sample.FrequencyAvg}";

                _writer.WriteLine(line);
                _writer.Flush();

                Console.WriteLine($"[SERVER] Sample primljen za vozilo {_currentVehicleId}, Row {sample.RowIndex}");
            }
            catch (Exception ex)
            {
                string rejectLine = $"{sample.RowIndex},\"{ex.Message}\",{SerializeSample(sample)}";
                _rejectWriter.WriteLine(rejectLine);
                _rejectWriter.Flush();

                Console.WriteLine($"[SERVER] Sample odbijen (Row {sample.RowIndex}) → {ex.Message}");
            }
        }

        public void EndSession(string vehicleId)
        {
            Console.WriteLine($"[DEBUG SERVER] EndSession primljen za VehicleId: {vehicleId}");
            Console.WriteLine($"[DEBUG SERVER] Trenutni VehicleId: {_currentVehicleId}");

            if (vehicleId != _currentVehicleId)
                throw new FaultException("Ne poklapa se VehicleId sa trenutnom sesijom!");

            _writer?.Dispose();
            _rejectWriter?.Dispose();

            _writer = null;
            _rejectWriter = null;

            Console.WriteLine($"[SERVER] EndSession za vozilo {vehicleId}");
        }

        private void ValidateSample(ChargingSample sample)
        {
            if (sample.Timestamp == default || sample.Timestamp > DateTime.Now)
                throw new FaultException("Nevalidan Timestamp!");

            if (sample.VoltageRmsAvg <= 0 || sample.CurrentRmsAvg < 0 || sample.FrequencyAvg <= 0)
                throw new FaultException("Nevalidne vrednosti signala!");
        }

        private string SerializeSample(ChargingSample s)
        {
            return $"{s.Timestamp:O},{s.VoltageRmsAvg},{s.CurrentRmsAvg},{s.RealPowerAvg},{s.FrequencyAvg}";
        }
    }
}
