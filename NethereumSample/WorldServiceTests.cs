using Nethereum.Contracts;
using Nethereum.Mud.Contracts.World;
using Nethereum.Mud.EncodingDecoding;
using Nethereum.Mud.TableRepository;
using Nethereum.RPC.Eth.DTOs;
// using Nethereum.XUnitEthereumClients;
using Nethereum.Hex.HexConvertors.Extensions;
using System.Diagnostics;
using Nethereum.Mud.IntegrationTests.MudTest.Tables;
using Nethereum.Mud.IntegrationTests.MudTest.Systems;
using Nethereum.Mud.IntegrationTests.MudTest.Systems.IncrementSystem;
using Nethereum.Mud.IntegrationTests.MudTest.Systems.IncrementSystem.ContractDefinition;
using Nethereum.Mud.Contracts;
using Nethereum.Mud.Contracts.Core.StoreEvents;
using Nethereum.Mud.Contracts.World.Systems.RegistrationSystem;
using Nethereum.Mud.Contracts.World.Systems.RegistrationSystem.ContractDefinition;
using Nethereum.Mud.Contracts.World.Tables;
using Nethereum.Mud.Contracts.Store.Tables;
using Nethereum.Mud.Contracts.Core.Systems;
using Nethereum.Mud.Contracts.Core.Namespaces;
using Nethereum.Web3;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Util;
using Nethereum.Mud.Contracts.World.Systems.AccessManagementSystem;
using Nethereum.ABI;
using Nethereum.RLP;
using System.Reactive.Linq;
using System.Threading;
using Nethereum.Mud;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nethereum.RPC.Reactive.Eth.Subscriptions;
using Nethereum.Web3.Accounts;
using Nethereum.JsonRpc.WebSocketClient;
using Nethereum.JsonRpc.WebSocketStreamingClient;
using Nethereum.Mud.IntegrationTests.MudTest;


namespace Nethereum.Mud.IntegrationTests
{
    //This tests are using a vanilla world contract (using vanilla template) deployed to anvil
    //and this configuration
    //is used for the tests
    /*
      export default defineWorld({
           tables: {
             Counter: {
               schema: {
                 value: "uint32",
               },
               key: [],
             },
             Item:{
               schema:{
                 id:"uint32",
                 price:"uint32",
                 name:"string",
                 description:"string",
                 owner:"string",
               },
               key:["id"]
             }
           },
         });
     */
    public class WorldServiceTests
    {
        public const string WorldAddress = "0xaf318b1edfa10f2d7a5a6a8aac36921cd0cda264";
        public const string WorldUrl = "http://localhost:8545";
        //using default anvil private key
        public const string OwnerPK = "0xac0974bec39a17e36ba4a6b4d238ff944bacb478cbed5efcae784d7bf4f2ff80";
        public const string UserPK = "0x59c6995e998f97a5a0044966f0945389dc9e86dae88c7a8412f4603b6b78690d";
        public const string UserAccount = "0x70997970C51812dc3A010C7d01b50e0d17dc79C8";

        public WorldService GetWorldService()
        {
            var web3 = new Nethereum.Web3.Web3(new Nethereum.Web3.Accounts.Account(OwnerPK), WorldUrl);
            return new WorldService(web3, WorldAddress);
        }

        public IWeb3 GetWeb3()
        {
            return new Nethereum.Web3.Web3(new Nethereum.Web3.Accounts.Account(OwnerPK), WorldUrl);
        }

        public WorldService GetUserWorldService()
        {
            var web3 = new Nethereum.Web3.Web3(new Nethereum.Web3.Accounts.Account(UserPK), WorldUrl);
            return new WorldService(web3, WorldAddress);
        }


        public async Task ShouldReturnRightVersion()
        {
            var worldService = GetWorldService();
            var storeVersion = await worldService.StoreVersionQueryAsStringAsync();
            // Assert.Equal("2.0.0", storeVersion);
        }


        [Function("increment")]
        public class IncrementFunction : FunctionMessage
        {
        }

        [Function("incrementwithrevert")]
        public class IncrementWithRevertFunction : FunctionMessage
        {
        }

        public void ShouldGenerateResourceIdsOnDifferentAbstractImplementations()
        {
            var counterTableResourceId = new CounterTableRecord().ResourceIdEncoded;
            var counterTableResourceId2 = ResourceRegistry.GetResourceEncoded<CounterTableRecord>();
            // Assert.Equal(counterTableResourceId.ToHex(), counterTableResourceId2.ToHex());

            var itemTableResourceId = new ItemTableRecord().ResourceIdEncoded;
            var itemTableResourceId2 = ResourceRegistry.GetResourceEncoded<ItemTableRecord>();
            // Assert.Equal(itemTableResourceId.ToHex(), itemTableResourceId2.ToHex());

            // Assert.NotEqual(counterTableResourceId.ToHex(), itemTableResourceId.ToHex());

        }



        public async Task ShouldGetRecord()
        {
            var worldService = GetWorldService();
            var receipt = await worldService.ContractHandler.SendRequestAndWaitForReceiptAsync(new IncrementFunction());
            var record = await worldService.GetRecordQueryAsync("Counter");
        }


        public async Task ShouldGetRecordUsingTable()
        {
            var worldService = GetWorldService();
            var receipt = await worldService.ContractHandler.SendRequestAndWaitForReceiptAsync(new IncrementFunction());
            var record = await worldService.GetRecordTableQueryAsync<CounterTableRecord, CounterTableRecord.CounterValue>(new CounterTableRecord());
            // Assert.True(record.Values.Value > 0);
        }




        public async Task ShouldSetAndGetAccessTable()
        {
            var worldService = GetWorldService();

            //first we are going to try to set the record on the resource table without direct table access
            var resourceAccess = new ResourceAccessTableRecord();
            resourceAccess.Keys.ResourceId = new CounterTableRecord().ResourceIdEncoded;
            resourceAccess.Keys.Caller = UserAccount;
            resourceAccess.Values.Access = true;
            try
            {
                var receipt = await worldService.SetRecordRequestAndWaitForReceiptAsync(resourceAccess);
            }
            catch (SmartContractCustomErrorRevertException e)
            {
                // Assert.True(e.IsCustomErrorFor<WorldAccessdeniedError>());  
                var error = e.DecodeError<WorldAccessdeniedError>();
                // Assert.Equal("tb:world:ResourceAccess", error.Resource);
                // Assert.True(error.Caller.IsTheSameAddress(worldService.Web3.TransactionManager.Account.Address));


            }

            //using the access management system
            //first owner account 
            var accessSystem = new AccessManagementSystemService(worldService.Web3, worldService.ContractAddress);
            var receipt2 = await accessSystem.GrantAccessRequestAndWaitForReceiptAsync(ResourceRegistry.GetResourceEncoded<CounterTableRecord>(), worldService.Web3.TransactionManager.Account.Address);
            var resourceAccessTable = new ResourceAccessTableRecord();
            resourceAccessTable.Keys.ResourceId = ResourceRegistry.GetResourceEncoded<CounterTableRecord>();
            resourceAccessTable.Keys.Caller = worldService.Web3.TransactionManager.Account.Address;
            var record = await worldService.GetRecordTableQueryAsync<ResourceAccessTableRecord, ResourceAccessTableRecord.ResourceAccessKey, ResourceAccessTableRecord.ResourceAccessValue>(resourceAccessTable);
            // Assert.True(record.Values.Access);

            //then another account
            var receipt3 = await accessSystem.GrantAccessRequestAndWaitForReceiptAsync(ResourceRegistry.GetResourceEncoded<CounterTableRecord>(), UserAccount);
            resourceAccessTable.Keys.Caller = UserAccount;
            record = await worldService.GetRecordTableQueryAsync<ResourceAccessTableRecord, ResourceAccessTableRecord.ResourceAccessKey, ResourceAccessTableRecord.ResourceAccessValue>(resourceAccessTable);
            // Assert.True(record.Values.Access);

            //we can now set the value using the user account directly in the table counter
            var counterTable = new CounterTableRecord();
            counterTable.Values.Value = 2500;
            var worldServiceUser = GetUserWorldService();

            receipt3 = await worldServiceUser.SetRecordRequestAndWaitForReceiptAsync(counterTable);
            var recordCounter = await worldService.GetRecordTableQueryAsync<CounterTableRecord, CounterTableRecord.CounterValue>(new CounterTableRecord());
            // Assert.True(recordCounter.Values.Value == 2500);

            //revoke access
            var receipt4 = await accessSystem.RevokeAccessRequestAndWaitForReceiptAsync(ResourceRegistry.GetResourceEncoded<CounterTableRecord>(), UserAccount);

            //we cannot set the value anymore
            var counterTable2 = new CounterTableRecord();
            counterTable2.Values.Value = 3000;
            try
            {
                var receipt5 = await worldServiceUser.SetRecordRequestAndWaitForReceiptAsync(counterTable2);
            }
            catch (SmartContractCustomErrorRevertException e)
            {
                // Assert.True(e.IsCustomErrorFor<WorldAccessdeniedError>());
                var error = e.DecodeError<WorldAccessdeniedError>();
                // Assert.Equal("tb:<root>:Counter", error.Resource);
                // Assert.True(error.Caller.IsTheSameAddress(UserAccount));
            }

            //but we can still set the value using the system as it is open
            var receipt6 = await worldServiceUser.ContractHandler.SendRequestAndWaitForReceiptAsync(new IncrementFunction());
            recordCounter = await worldService.GetRecordTableQueryAsync<CounterTableRecord, CounterTableRecord.CounterValue>(new CounterTableRecord());
            //the value should have been incremented by 1 of the previous value we set 2000
            // Assert.True(recordCounter.Values.Value == 2501);

        }

        public async Task ShouldCallIncrementWithRevert()
        {
            var worldService = GetWorldService();
            var recordCounter = await worldService.GetRecordTableQueryAsync<CounterTableRecord, CounterTableRecord.CounterValue>(new CounterTableRecord());

            Console.WriteLine("recordCounter: " + recordCounter.Values.Value);
            var web3 = GetWeb3();
            var mudTest = new MudTestNamespace(web3, WorldAddress);

            try
            {
                Console.WriteLine("Try Calling IncrementWithRevert");

                // Increment 
                // await mudTest.Systems.IncrementSystemService.IncrementRequestAndWaitForReceiptAsync();

                // Increment with revert
                await mudTest.Systems.IncrementSystemService.IncrementWithRevertRequestAndWaitForReceiptAsync();

                var counterRecord = await mudTest.Tables.CounterTableService.GetTableRecordAsync();
                Console.WriteLine("counter: " + counterRecord.Values.Value);

                Console.WriteLine("End Calling IncrementWithRevert");
            }
            catch (SmartContractCustomErrorRevertException e)
            {
                Console.WriteLine("Execute Catch block");
                if (e.IsCustomErrorFor<WorldAccessdeniedError>())
                {
                    var errorAccessDenied = e.DecodeError<WorldAccessdeniedError>();
                    Console.WriteLine("error: " + errorAccessDenied.Resource);
                }

                // Fully recommended
                var fullError = mudTest.FindCustomErrorException(e);
                Console.WriteLine(fullError.ErrorABI.Name);

                // Not recommended
                // if (e.IsCustomErrorFor<IncrementsystemCounterrevertError>())
                // {
                //     var errorCustomRevert = e.DecodeError<IncrementsystemCounterrevertError>();
                //     Console.WriteLine("error: " + errorCustomRevert);
                // }
            }
        }


        public async Task ShouldSetRecord()
        {
            var worldService = GetWorldService();
            var receipt = await worldService.SetRecordRequestAndWaitForReceiptAsync("Counter", ABIType.CreateABIType("int32").EncodePacked(8));
            var record = await worldService.GetRecordQueryAsync("Counter");
            // Assert.True(record.StaticData.ToBigIntegerFromRLPDecoded() == 8);
        }

        public async Task ShouldSetAndGetRecordTableItem()
        {
            var worldService = GetWorldService();
            var itemTable = new ItemTableRecord();
            itemTable.Values.Name = "Item1";
            itemTable.Values.Price = 100;
            itemTable.Values.Description = "Description";
            itemTable.Values.Owner = "Owner";
            itemTable.Keys.Id = 1;
            var receipt = await worldService.SetRecordRequestAndWaitForReceiptAsync(itemTable);
            var returnedItem = new ItemTableRecord();
            returnedItem.Keys.Id = 1;
            returnedItem = await worldService.GetRecordTableQueryAsync<ItemTableRecord, ItemTableRecord.ItemKey, ItemTableRecord.ItemValue>(returnedItem);
            // Assert.Equal("Item1", returnedItem.Values.Name);
            // Assert.Equal(100, returnedItem.Values.Price);
            // Assert.Equal("Description", returnedItem.Values.Description);
            // Assert.Equal("Owner", returnedItem.Values.Owner);
            // Assert.Equal(1, returnedItem.Keys.Id);
        }


        public async Task ShouldSetRecordTable()
        {
            var worldService = GetWorldService();
            var counterTable = new CounterTableRecord();
            counterTable.Values.Value = 10;
            var receipt = await worldService.SetRecordRequestAndWaitForReceiptAsync(counterTable);
            var record = await worldService.GetRecordQueryAsync("Counter");
        }


        public async Task ShouldGetSetRecordsFromLogs()
        {
            var web3 = GetWeb3();
            var storeLogProcessingService = new StoreEventsLogProcessingService(web3, WorldAddress);
            var setRecords = await storeLogProcessingService.GetAllSetRecordForTable("Counter", null, null, CancellationToken.None);

            var counterTable0 = new CounterTableRecord();
            counterTable0.DecodeValues(setRecords[0].Event.StaticData, setRecords[0].Event.EncodedLengths, setRecords[0].Event.DynamicData);
            // Assert.True(counterTable0.Values.Value > 0);

            var counterTable1 = new CounterTableRecord();
            counterTable1.DecodeValues(setRecords[1].Event.StaticData, setRecords[1].Event.EncodedLengths, setRecords[1].Event.DynamicData);
            // Assert.True(counterTable0.Values.Value > 0);

            foreach (var setRecord in setRecords)
            {
                var counterTable = new CounterTableRecord();
                counterTable.DecodeValues(setRecord.Event.StaticData, setRecord.Event.EncodedLengths, setRecord.Event.DynamicData);
                Console.WriteLine(counterTable.Values.Value);
                // Assert.True(counterTable.Values.Value > 0);
            }

        }



        public async Task ShouldGetAllChanges()
        {
            var web3 = GetWeb3();
            var storeLogProcessingService = new StoreEventsLogProcessingService(web3, WorldAddress);
            var inMemoryStore = new InMemoryTableRepository();
            var tableId = new CounterTableRecord().ResourceIdEncoded;
            await storeLogProcessingService.ProcessAllStoreChangesAsync(inMemoryStore, null, null, CancellationToken.None);
            var results = await inMemoryStore.GetTableRecordsAsync<CounterTableRecord>(tableId);

            // Assert.True(results.ToList()[0].Values.Value> 0);

            foreach (var result in results)
            {
                Console.WriteLine("value: " + result.Values.Value);
            }


            var resultsSystems = await inMemoryStore.GetTableRecordsAsync<SystemsTableRecord>(new SystemsTableRecord().ResourceIdEncoded);
            // Assert.True(resultsSystems.ToList().Count > 0);
            foreach (var result in resultsSystems)
            {
                // Console.WriteLine(ResourceEncoder.Decode(result.Keys.SystemId).Name);
                // Console.WriteLine(ResourceEncoder.Decode(result.Keys.SystemId).Namespace);
            }

            var resultsAccess = await inMemoryStore.GetTableRecordsAsync<ResourceAccessTableRecord>(new ResourceAccessTableRecord().ResourceIdEncoded);
            // Assert.True(resultsAccess.ToList().Count > 0);
            foreach (var result in resultsAccess)
            {
                // Console.WriteLine(ResourceEncoder.Decode(result.Keys.ResourceId).Name);
                // Console.WriteLine(result.Keys.Caller);
                // Console.WriteLine(result.Values.Access);
            }


            //the world factory is the owner of the store and world namespaces
            var namespaceOwner = await inMemoryStore.GetTableRecordsAsync<NamespaceOwnerTableRecord>(new NamespaceOwnerTableRecord().ResourceIdEncoded);
            // Assert.True(namespaceOwner.ToList().Count > 0);
            foreach (var result in namespaceOwner)
            {
                // Console.WriteLine(ResourceEncoder.Decode(result.Keys.NamespaceId).Name);
                // Console.WriteLine(ResourceEncoder.Decode(result.Keys.NamespaceId).Namespace);
                // Console.WriteLine(result.Values.Owner);
            }

            var itemTableResults = await inMemoryStore.GetTableRecordsAsync<ItemTableRecord>(new ItemTableRecord().ResourceIdEncoded);
            // Assert.True(itemTableResults.ToList().Count >= 0); // we many not have set a record yet
            foreach (var result in itemTableResults)
            {
                Console.WriteLine(result.Keys.Id);
                Console.WriteLine(result.Values.Name);
                Console.WriteLine(result.Values.Price);
                Console.WriteLine(result.Values.Description);
                Console.WriteLine(result.Values.Owner);
            }

        }


        public async Task ShouldGetAllChangesForASingleTable()
        {
            var web3 = GetWeb3();
            var storeLogProcessingService = new StoreEventsLogProcessingService(web3, WorldAddress);
            var inMemoryStore = new InMemoryTableRepository();
            var tableId = new CounterTableRecord().ResourceIdEncoded;
            await storeLogProcessingService.ProcessAllStoreChangesAsync(inMemoryStore, tableId, null, null, CancellationToken.None);
            var results = await inMemoryStore.GetTableRecordsAsync<CounterTableRecord>(tableId);

            // Assert.True(results.ToList()[0].Values.Value > 0);

        }

        public async Task ShouldGetAllTablesRegistered()
        {
            var web3 = GetWeb3();
            var storeLogProcessingService = new StoreEventsLogProcessingService(web3, WorldAddress);
            var inMemoryStore = new InMemoryTableRepository();
            var tableId = new TablesTableRecord().ResourceIdEncoded;
            await storeLogProcessingService.ProcessAllStoreChangesAsync(inMemoryStore, tableId, null, null, CancellationToken.None);
            var results = await inMemoryStore.GetTableRecordsAsync<TablesTableRecord>(tableId);

            // Assert.True(results.ToList().Count > 0);

            foreach (var result in results)
            {
                var recordTableId = result.Keys.GetTableIdResource();

                if (recordTableId.Name == "Counter")
                {
                    // Assert.True(recordTableId.IsRoot());
                    var field = result.Values.GetValueSchemaFields().ToList()[0];
                    // Assert.Equal("uint32", field.Type);
                    // Assert.Equal(1, field.Order);
                    // Assert.Equal("value", field.Name);
                }

                if (recordTableId.Name == "Item")
                {
                    // Assert.True(recordTableId.IsRoot());
                    var fields = result.Values.GetValueSchemaFields().ToList();

                    // Assert.Equal("uint32", fields[0].Type);
                    // Assert.Equal(1, fields[0].Order);
                    // Assert.Equal("price", fields[0].Name);
                    // Assert.Equal("string", fields[1].Type);
                    // Assert.Equal(2, fields[1].Order);
                    // Assert.Equal("name", fields[1].Name);
                    // Assert.Equal("string", fields[2].Type);
                    // Assert.Equal(3, fields[2].Order);
                    // Assert.Equal("description", fields[2].Name);
                    // Assert.Equal("string", fields[3].Type);
                    // Assert.Equal(4, fields[3].Order);
                    // Assert.Equal("owner", fields[3].Name);

                    var keys = result.Values.GetKeySchemaFields().ToList();
                    // Assert.Equal("uint32", keys[0].Type);
                    // Assert.Equal(1, keys[0].Order);
                    // Assert.Equal("id", keys[0].Name);

                }

                Console.WriteLine(recordTableId.Name);
                Console.WriteLine(recordTableId.Namespace);
                Console.WriteLine("Field schema");
                foreach (var field in result.Values.GetValueSchemaFields())
                {
                    Console.WriteLine(field.Name);
                    Console.WriteLine(field.Type);
                    Console.WriteLine(field.Order);

                }
                Console.WriteLine("Key Schema");
                foreach (var field in result.Values.GetKeySchemaFields())
                {
                    Console.WriteLine(field.Name);
                    Console.WriteLine(field.Type);
                    Console.WriteLine(field.Order);
                }

            }
        }

        // public async Task MonitorTableChanges()
        // {
        //     Console.WriteLine("Starting MonitorTableChanges");
        //     // Initialize Web3 and the services (assume WorldAddress is defined elsewhere)
        //     var web3 = GetWeb3();
        //     var storeLogProcessingService = new StoreEventsLogProcessingService(web3, WorldAddress);
        //     var inMemoryStore = new InMemoryTableRepository();

        //     // Define the Table ID you are interested in
        //     var tableId = new CounterTableRecord().ResourceIdEncoded;

        //     // Create an observable that listens to changes in the table
        //     var observable = Observable.Create<CounterTableRecord>(async observer =>
        //     {
        //         await storeLogProcessingService.ProcessAllStoreChangesAsync(inMemoryStore, tableId, null, null, CancellationToken.None);
        //         var results = await inMemoryStore.GetTableRecordsAsync<CounterTableRecord>(tableId);

        //         // Emit each record to the observer
        //         foreach (var result in results)
        //         {
        //             observer.OnNext(result);
        //         }

        //         // Complete the sequence after processing current records
        //         observer.OnCompleted();

        //         return () => { /* Dispose or clean up resources if needed */ };
        //     });

        //     // Subscribe to the observable to react to new data
        //     observable.Subscribe(
        //         record =>
        //         {
        //             // React to new record, e.g., print or process it further
        //             Console.WriteLine($"New record value: {record.Values.Value}");
        //         },
        //         ex => Console.WriteLine($"Error: {ex.Message}"),
        //         () => Console.WriteLine("Completed processing all records.")
        //     );

        //     // Keep the application running (you may have another event loop or mechanism)
        //     Console.ReadLine();
        // }

        private static ulong _lastProcessedBlock = 0;
        private readonly string _webSocketUrl = "ws://localhost:8545"; // Assuming you're using a local Ethereum node

        public async Task MonitorTableChangesRealTime()
        {
            Console.WriteLine("Starting MonitorTableChangesRealTime");
            // Initialize Nethereum's StreamingWebSocketClient
            using (var webSocketClient = new StreamingWebSocketClient(_webSocketUrl))
            {
                var storeLogProcessingService = new StoreEventsLogProcessingService(new Nethereum.Web3.Web3(_webSocketUrl), WorldAddress);
                var inMemoryStore = new InMemoryTableRepository();

                // Define the Table ID
                var tableId = new CounterTableRecord().ResourceIdEncoded;

                // Create a block header subscription
                var blockHeaderSubscription = new EthNewBlockHeadersObservableSubscription(webSocketClient);

                // Subscribe to the block header stream
                blockHeaderSubscription.GetSubscriptionDataResponsesAsObservable()
                    .Select(blockHeader => (ulong)blockHeader.Number.Value) // Cast BigInteger to ulong
                    .Subscribe(async newBlockNumber =>
                    {
                        // Process new changes on each new block
                        await storeLogProcessingService.ProcessAllStoreChangesAsync(
                            inMemoryStore,
                            tableId,
                            _lastProcessedBlock,  // Fetch changes from the last processed block
                            newBlockNumber,       // Until current block
                            CancellationToken.None
                        );

                        // Update the last processed block
                        _lastProcessedBlock = newBlockNumber;

                        // Fetch and react to new data
                        var results = await inMemoryStore.GetTableRecordsAsync<CounterTableRecord>(tableId);
                        foreach (var result in results)
                        {
                            Console.WriteLine($"New Counter Value: {result.Values.Value}");
                        }
                    },
                    ex => Console.WriteLine($"Error: {ex.Message}"));

                // Open WebSocket connection and start the subscription
                await webSocketClient.StartAsync();
                await blockHeaderSubscription.SubscribeAsync();

                Console.WriteLine("Subscribed to new block headers. Press any key to exit.");
                Console.ReadKey();

                // Unsubscribe and stop WebSocket connection before exiting
                await blockHeaderSubscription.UnsubscribeAsync();
            }
        }
    }
}