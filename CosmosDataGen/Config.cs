#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8603 // Possible null reference return.

using CommandLine;
using Newtonsoft.Json;

namespace CosmosDataGen
{
    public class Config
    {
        [Option('e', Required = true, HelpText = "Cosmos account end point")]
        public string EndPoint { get; set; }

        [Option('k', Required = true, HelpText = "Cosmos account master key")]
        [JsonIgnore]
        public string Key { get; set; }

        [Option(Required = false, HelpText = "Database to use")]
        public string Database { get; set; } = "db";

        [Option(Required = false, HelpText = "Container to use")]
        public string Container { get; set; } = "data";

        [Option('t', Required = false, HelpText = "Container throughput use")]
        public int Throughput { get; set; } = 4000;

        [Option('n', Required = false, HelpText = "Number of documents to insert")]
        public int ItemCount { get; set; } = 200000;

        [Option("dp", Required = false, HelpText = "Degree of parallism")]
        public int DegreeOfParallelism { get; set; } = -1;

        internal int GetTaskCount(int containerThroughput)
        {
            int taskCount = this.DegreeOfParallelism;
            if (taskCount == -1)
            {
                // set TaskCount = 10 for each 10k RUs, minimum 1, maximum { #processor * 50 }
                taskCount = Math.Max(containerThroughput / 1000, 1);
                taskCount = Math.Min(taskCount, Environment.ProcessorCount * 50);
            }

            return taskCount;
        }

        internal void Print()
        {
            using (ConsoleColorContext ct = new ConsoleColorContext(ConsoleColor.Green))
            {
                Console.WriteLine("Parameters:");
                Console.WriteLine(JsonHelper.ToString(this));
            }
        }

        internal static Config From(string[] args)
        {
            Config options = null;

            Parser parser = new Parser((settings) =>
            {
                settings.CaseSensitive = false;
                settings.HelpWriter = Console.Error;
                settings.AutoHelp = false;
                settings.AutoVersion = false;
                settings.HelpWriter = null;
            });

            parser.ParseArguments<Config>(args)
                .WithParsed<Config>(e => options = e)
                .WithNotParsed<Config>(e => Config.HandleParseError(e));

            return options;
        }

        private static void HandleParseError(IEnumerable<Error> errors)
        {

            // Display Help
            Console.WriteLine("Command Line.\n");
            Console.WriteLine("  -e                  Required.Cosmos account end point");
            Console.WriteLine("  -k                  Required.Cosmos account master key");
            Console.WriteLine("  --database          Database to use");
            Console.WriteLine("  --container         Container to use");
            Console.WriteLine("  -t                  Container throughput use");
            Console.WriteLine("  -n                  Number of documents to insert");
            Console.WriteLine("  -p                  Degree of parallism");

            using (ConsoleColorContext ct = new ConsoleColorContext(ConsoleColor.Red))
            {
                foreach (Error e in errors)
                {
                    Console.WriteLine(e.ToString());
                }
            }

            Environment.Exit(errors.Count());
        }
    }
}
