using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public static class Logger
    {
        private static readonly string LogFile = "client_errors.log";

        public static void Log(string message)
        {
            File.AppendAllText(LogFile, $"{DateTime.Now}: {message}{Environment.NewLine}");
        }
    }
}
