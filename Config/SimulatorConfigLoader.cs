using System.IO;
using System.Text.Json;

namespace MaterialControlSimulator
{
    public static class SimulatorConfigLoader
    {
        public static SimulatorConfig Load()
        {
            const string path = "simulator.json";

            if (!File.Exists(path))
                return new SimulatorConfig();

            string json =
                File.ReadAllText(path);

            return JsonSerializer.Deserialize<SimulatorConfig>(json)
                   ?? new SimulatorConfig();
        }
    }
}
