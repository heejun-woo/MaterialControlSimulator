using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialControlSimulator
{
    public static class Logger
    {
        public static event Action<string>? MessageReceived;


        public static void Write(string message)
        {
            var log =
                $"[{DateTime.Now:HH:mm:ss}] {message}";


            MessageReceived?.Invoke(log);
        }


        public static void Info(string message)
        {
            Write($"INFO : {message}");
        }


        public static void Warn(string message)
        {
            Write($"WARN : {message}");
        }


        public static void Error(string message)
        {
            Write($"ERROR : {message}");
        }
    }
}
