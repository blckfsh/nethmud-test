using Nethereum.Mud.IntegrationTests;


var service = new WorldServiceTests();
await service.ShouldGetAllChanges();