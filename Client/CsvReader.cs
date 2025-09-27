using Common.Contracts;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public class CsvReader
    {
        private readonly string _filePath;
        private readonly string _vehicleId;

        public CsvReader(string filePath, string vehicleId)
        {
            _filePath = filePath;
            _vehicleId = vehicleId;
        }

        public List<ChargingSample> ReadRows()
        {
            var result = new List<ChargingSample>();
            int rowIndex = 0;

            using (var reader = new StreamReader(_filePath))
            {
                string line;

                if (!reader.EndOfStream)
                    reader.ReadLine();

                while ((line = reader.ReadLine()) != null)
                {
                    rowIndex++;
                    try
                    {
                        var columns = line.Split(',');

                        // Čistimo suvišne razmake
                        for (int i = 0; i < columns.Length; i++)
                            columns[i] = columns[i].Trim();

                        var sample = new ChargingSample
                        {
                            RowIndex = rowIndex,
                            VehicleId = _vehicleId,
                            Timestamp = DateTime.ParseExact(columns[0], "yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture),

                            // Voltage RMS
                            VoltageRmsMin = double.Parse(columns[1], CultureInfo.InvariantCulture),
                            VoltageRmsAvg = double.Parse(columns[2], CultureInfo.InvariantCulture),
                            VoltageRmsMax = double.Parse(columns[3], CultureInfo.InvariantCulture),

                            // Current RMS
                            CurrentRmsMin = double.Parse(columns[4], CultureInfo.InvariantCulture),
                            CurrentRmsAvg = double.Parse(columns[5], CultureInfo.InvariantCulture),
                            CurrentRmsMax = double.Parse(columns[6], CultureInfo.InvariantCulture),

                            // Real Power
                            RealPowerMin = double.Parse(columns[7], CultureInfo.InvariantCulture),
                            RealPowerAvg = double.Parse(columns[8], CultureInfo.InvariantCulture),
                            RealPowerMax = double.Parse(columns[9], CultureInfo.InvariantCulture),

                            // Frequency (pretpostavljamo da su kolone 17,18,19)
                            FrequencyMin = double.Parse(columns[16], CultureInfo.InvariantCulture),
                            FrequencyAvg = double.Parse(columns[17], CultureInfo.InvariantCulture),
                            FrequencyMax = double.Parse(columns[18], CultureInfo.InvariantCulture)
                        };

                        result.Add(sample);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"Row {rowIndex} error: {ex.Message} → {line}");
                    }
                }
            }


            return result;
        }
    }
}
