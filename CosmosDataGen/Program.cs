using System.Diagnostics;
using System.Net;
using Microsoft.Azure.Cosmos;

namespace CosmosDataGen
{
    public sealed class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("CosmosDataGen\n");

                Config config = Config.From(args);

                ThreadPool.SetMinThreads(100,100);

                config.Print();

                Program program = new Program();

                await program.ExecuteAsync(config);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
            }
            finally
            {
                if (Debugger.IsAttached)
                {
                    Console.WriteLine("Press enter to exit...");
                    Console.ReadLine();
                }
            }
        }

        private async Task ExecuteAsync(Config config)
        {
            using (CosmosClient cosmosClient = CreateCosmosClient(config))
            {
                Console.WriteLine("Connected Cosmos DB Client: {0}", cosmosClient.Endpoint);

                Microsoft.Azure.Cosmos.Database database = await cosmosClient.CreateDatabaseIfNotExistsAsync(config.Database);
                Console.WriteLine("Connected to Database: {0}", database.Id);

                ContainerResponse containerResponse = await Program.CreatePartitionedContainerAsync(config, database);
                Container container = containerResponse;
                Console.WriteLine("Connected to Container: {0}", container.Id);

                int? currentContainerThroughput = await container.ReadThroughputAsync();

                if (!currentContainerThroughput.HasValue)
                {
                    ThroughputResponse throughputResponse = await database.ReadThroughputAsync(requestOptions: null);
                    throw new InvalidOperationException($"Using database {config.Database} with {throughputResponse.Resource.Throughput} RU/s. " +
                        $"Container {config.Container} must have a configured throughput.");
                }

                Console.WriteLine($"Using container {config.Container} with {currentContainerThroughput} RU/s");
                
                int taskCount = config.GetTaskCount(currentContainerThroughput.Value);

                int opsPerTask = config.ItemCount / taskCount;

                Console.WriteLine("Ready to initiate inserts with {0} tasks {1} Inserts per task, total of {2} items will be inserted ", taskCount, opsPerTask, config.ItemCount);

                Console.WriteLine("Press Enter to initiate, CTRL+C to abort");
                Console.ReadLine();

                List<Task<RunSummary>> ts = new List<Task<RunSummary>>();

                for (int i = 0; i < taskCount; i++)
                    ts.Add(TaskRunner(cosmosClient, database, container, opsPerTask, i));

                Task.WaitAll(ts.ToArray());

                foreach (Task<RunSummary> t in ts)
                    Console.WriteLine("Completed Task Id:{0} Inserts:{1} RuCharges:{2:0.00}", t.Result.taskId, t.Result.inserts, t.Result.RUCharges);

                Console.WriteLine("done");
            }
        }

        private async Task<RunSummary> TaskRunner(CosmosClient cc, Database database, Container container, int inserts, int taskid)
        {
            double reqcharges = 0;
            int ins = 0;

            Console.WriteLine("Starting Task Runner ID:{0}", taskid);

            for (int i = 0; i < inserts; i++)
            {
                Family family = new Family
                {
                    Id = Guid.NewGuid().ToString(),
                    LastName = Guid.NewGuid().ToString(),
                    Parents = new Parent[]
                    {
                    new Parent { FirstName = Guid.NewGuid().ToString(), FamilyName=Guid.NewGuid().ToString()},
                    new Parent { FirstName = Guid.NewGuid().ToString(), FamilyName=Guid.NewGuid().ToString()}
                            },
                            Children = new Child[]
                            {
                    new Child
                    {
                        FirstName = Guid.NewGuid().ToString(),
                        FamilyName = Guid.NewGuid().ToString(),
                        Gender = Guid.NewGuid().ToString(),
                        Grade = (new Random()).Next(1,12),
                        Pets = new Pet[]
                        {
                            new Pet { GivenName = Guid.NewGuid().ToString() }
                        }
                    }
                    },
                    Address = new Address { State = Guid.NewGuid().ToString(), County = Guid.NewGuid().ToString(), City = Guid.NewGuid().ToString() },
                };

                ItemResponse<Family> resp = await container.CreateItemAsync<Family>(family, new PartitionKey(family.LastName));

                ins += 1;
                reqcharges += resp.RequestCharge;

                if(ins % 100 == 0)
                    Console.WriteLine("Task Id:{0} Inserts:{1} RuCharges:{2:0.00}", taskid, ins, reqcharges);
            }
            return new RunSummary() { taskId = taskid, inserts = ins, RUCharges = reqcharges };
        }

        internal struct RunSummary
        {
            public int taskId { get; set; }
            public int inserts { get; set; }
            public double RUCharges { get; set; }
        }

        private CosmosClient CreateCosmosClient(Config config)
        {
            CosmosClientOptions clientOptions = new Microsoft.Azure.Cosmos.CosmosClientOptions()
            {
                ApplicationName = "CosmosDataGen",
                MaxRetryAttemptsOnRateLimitedRequests = 0
            };

            return new CosmosClient(
                        config.EndPoint,
                        config.Key,
                        clientOptions);
        }

        private static async Task<ContainerResponse> CreatePartitionedContainerAsync(Config options, Database database)
        {
            Container container = database.GetContainer(options.Container);

            try
            {
                return await container.ReadContainerAsync();
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                double estimatedCostPerMonth = 0.12 * options.Throughput;
                double estimatedCostPerHour = estimatedCostPerMonth / (24 * 30);
                Console.WriteLine($"The container will cost an estimated ${Math.Round(estimatedCostPerHour, 2)} per hour (${Math.Round(estimatedCostPerMonth, 2)} per month)");
                Console.WriteLine("Press enter to continue, CTRL+C to abort");
                Console.ReadLine();

                string partitionKeyPath = "/LastName";
                return await database.CreateContainerAsync(options.Container, partitionKeyPath, options.Throughput);
            }
        }
    }
}