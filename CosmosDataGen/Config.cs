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

        [Option("cp", Required = false, HelpText = "Connection Policy: Direct|Gateway")]
        public string ConnectionPolicy { get; set; } = "Direct";

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
            Console.WriteLine("  -e                  Required. Azure Cosmos DB account endpoint URI");
            Console.WriteLine("  -k                  Required. Azure Cosmos DB account Read-write access key");
            Console.WriteLine("  --database          Target Database to use: default \"db\"");
            Console.WriteLine("  --container         Target Container to use: default \"data\"");
            Console.WriteLine("  -t                  Container throughput: default 4000");
            Console.WriteLine("  -n                  Number of documents to insert: default 200000");
            Console.WriteLine("  --dp                Number of parallel tasks to create: default -1 (automatic), 1 = Just one task");
            Console.WriteLine("  --cp                Connection policy: Direct or Gateway connection mode: Default Direct");
            Console.WriteLine("\nExample:");
            Console.WriteLine("\nCOSMOSDBGEN -e uri -k accesskey [--database db] [--container data] [-t 4000] [-n 200000] [--dp -1] [--cp Direct|Gateway]\n\n");

            using (ConsoleColorContext ct = new ConsoleColorContext(ConsoleColor.Red))
            {
                foreach (Error e in errors)
                {
                    Console.WriteLine("Command Line error:{0}", e.ToString());
                }
            }

            Environment.Exit(errors.Count());
        }
    }
}
