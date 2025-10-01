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
        private bool _isTransferring;

        public delegate void TransferEventHandler(string vehicleId);
        public delegate void SampleEventHandler(string vehicleId, ChargingSample sample);
        public delegate void WarningEventHandler(string vehicleId, string warning);

        public static event TransferEventHandler OnTransferStarted;
        public static event SampleEventHandler OnSampleReceived;
        public static event TransferEventHandler OnTransferCompleted;
        public static event WarningEventHandler OnWarningRaised;

        private double _cumulativeEnergy = 0;       // kumulativna energija (sumira RealPowerAvg)
        private double _lastEnergy = 0;             // poslednja vrednost energije
        private int _stagnationCounter = 0;         // broj uzastopnih stagnacija
        private const double OverloadThreshold = 6.0; // prag za overload 

        private double _lastFrequencyMin = 50;
        private double _lastFrequencyMax = 50;
        private const double FrequencyDeviationThreshold = 0.5; // odstupanje od 50 Hz
        private const double FrequencySpikeThreshold = 0.05;     // prag za nagli skok (primer: 2 Hz)

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

            _isTransferring = true;

            _cumulativeEnergy = 0;
            _lastEnergy = 0;
            _stagnationCounter = 0;

            _lastFrequencyMin = 50;
            _lastFrequencyMax = 50;

            Console.WriteLine($"[SERVER STATUS] Prenos započet za vozilo {vehicleId}");
            OnTransferStarted?.Invoke(vehicleId);
            //Console.WriteLine($"[SERVER] StartSession za vozilo {vehicleId}");

        }

        public void PushSample(ChargingSample sample)
        {
            if (!_isTransferring)
            {
                Console.WriteLine("[SERVER STATUS] Greška: primljen sample bez aktivne sesije!");
                throw new FaultException("Nema aktivne sesije!");
            }

            Console.WriteLine("[SERVER STATUS] Prenos u toku...");

            try
            {
                ValidateSample(sample);

                string line = $"{sample.RowIndex},{sample.Timestamp:O}," +
                              $"{sample.VoltageRmsAvg},{sample.CurrentRmsAvg}," +
                              $"{sample.RealPowerAvg},{sample.FrequencyAvg}";

                _writer.WriteLine(line);
                _writer.Flush();

                Console.WriteLine($"[SERVER] Sample primljen za vozilo {_currentVehicleId}, Row {sample.RowIndex}");
                OnSampleReceived?.Invoke(_currentVehicleId, sample);

                //Analitika 1
                _cumulativeEnergy += sample.RealPowerAvg;

                // provera stagnacije (ako energija ne raste)
                if (Math.Abs(_cumulativeEnergy - _lastEnergy) < 0.0001)
                {
                    _stagnationCounter++;
                    if (_stagnationCounter > 10)
                    {
                        Console.WriteLine($"[WARNING] EnergyStallWarning (vozilo {_currentVehicleId})");
                        OnWarningRaised?.Invoke(_currentVehicleId, "EnergyStallWarning");
                        _stagnationCounter = 0; // reset posle upozorenja
                    }
                } 
                else
                {
                    _stagnationCounter = 0;
                    _lastEnergy = _cumulativeEnergy;
                }

                // provera overload-a
                if (sample.RealPowerMax > OverloadThreshold)
                {
                    Console.WriteLine($"[WARNING] OverloadWarning (vozilo {_currentVehicleId})");
                    OnWarningRaised?.Invoke(_currentVehicleId, "OverloadWarning");
                }

                //Analitika 2
                if (Math.Abs(sample.FrequencyAvg - 50) > FrequencyDeviationThreshold)
                {
                    Console.WriteLine($"[WARNING] FrequencyDeviationWarning (vozilo {_currentVehicleId}) → Avg={sample.FrequencyAvg}");
                    OnWarningRaised?.Invoke(_currentVehicleId, "FrequencyDeviationWarning");
                }

                if (Math.Abs(sample.FrequencyMin - _lastFrequencyMin) > FrequencySpikeThreshold ||
                    Math.Abs(sample.FrequencyMax - _lastFrequencyMax) > FrequencySpikeThreshold)
                {
                    Console.WriteLine($"[WARNING] FrequencySpike (vozilo {_currentVehicleId})");
                    OnWarningRaised?.Invoke(_currentVehicleId, "FrequencySpike");
                }

                _lastFrequencyMin = sample.FrequencyMin;
                _lastFrequencyMax = sample.FrequencyMax;
            }
            catch (Exception ex)
            {
                string rejectLine = $"{sample.RowIndex},\"{ex.Message}\",{SerializeSample(sample)}";
                _rejectWriter.WriteLine(rejectLine);
                _rejectWriter.Flush();

                Console.WriteLine($"[SERVER] Sample odbijen (Row {sample.RowIndex}) → {ex.Message}");
                OnWarningRaised?.Invoke(_currentVehicleId, $"Sample odbijen: {ex.Message}");
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

            _isTransferring = false;
            Console.WriteLine($"[SERVER STATUS] Prenos završen za vozilo {vehicleId}");
            OnTransferCompleted?.Invoke(vehicleId);
            // Console.WriteLine($"[SERVER] EndSession za vozilo {vehicleId}");
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
