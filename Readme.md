# CosmosDataGen

## High-Performance Azure Cosmos DB dummy data generator

This utility uses multiple parallel tasks to fill up an Azure Cosmos DB container fast and efficiently with dummy data for testing purposes.

Possible uses:  
    - Create a container filled with data for testing disaster recovery scenarios  
    - Generate a container with data to test periodic backup scenarios  
    - Generate a container with data to test continuous backup  
    - Generate dummy data to test application performance  

## Usage:

```
COSMOSDBGEN -e uri -k accesskey [--database db] [--container data] [-t 4000] [-n 200000] [--dp -1] [--cp Direct|Gateway]

-e              Required. Azure Cosmos DB account endpoint URI
-k              Required. Azure Cosmos DB account Read-write access key
--database      Target Database to use: default "db"
--container     Target Container to use: default "data"
-t              Container throughput: default 4000
-n              Number of documents to insert: default 200000
--dp            Number of parallel tasks to create: default -1 (automatic), 1 = Just one task"
--cp            Connection policy: Use Direct or Gateway connection mode
```

## External Nuget Dependencies:

    Include="CommandLineParser" Version="2.8.0" 
    Include="Microsoft.Azure.Cosmos" Version="3.23.0"
    Include="Newtonsoft.Json" Version="13.0.1"

## Example:

```
cosmosdatagen -e https://cosmosdb.documents.azure.com:443/ -k youraccesskey== -n 800"

CosmosDataGen

Parameters:
{
  "EndPoint": "https://cosmosdb.documents.azure.com:443/",
  "Database": "db",
  "Container": "data",
  "Throughput": 4000,
  "ItemCount": 800,
  "DegreeOfParallelism": -1
}
Connected Cosmos DB Client: https://cosmosdb.documents.azure.com/
Connected to Database: db
The container will cost an estimated $0.67 per hour ($480 per month)
Press enter to continue, CTRL+C to abort

Connected to Container: data
Using container data with 4000 RU/s
Ready to initiate inserts with 4 tasks 200 Inserts per task, total of 800 items will be inserted
Press Enter to initiate, CTRL+C to abort

Starting Task Runner ID:0
Starting Task Runner ID:1
Starting Task Runner ID:2
Starting Task Runner ID:3
Task Id:1 Inserts:100 RuCharges:1295.00
Task Id:2 Inserts:100 RuCharges:1295.00
Task Id:0 Inserts:100 RuCharges:1295.00
Task Id:3 Inserts:100 RuCharges:1295.00
Task Id:0 Inserts:200 RuCharges:2590.00
Task Id:2 Inserts:200 RuCharges:2590.00
Task Id:1 Inserts:200 RuCharges:2590.00
Task Id:3 Inserts:200 RuCharges:2590.00
Completed Task Id:0 Inserts:200 RuCharges:2590.00
Completed Task Id:1 Inserts:200 RuCharges:2590.00
Completed Task Id:2 Inserts:200 RuCharges:2590.00
Completed Task Id:3 Inserts:200 RuCharges:2590.00
done

```
