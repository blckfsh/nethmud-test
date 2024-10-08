using Nethereum.Mud.IntegrationTests;
using System.Reactive;


var service = new WorldServiceTests();
// await service.ShouldGetAllChanges();
// await service.ShouldGetSetRecordsFromLogs();
// await service.MonitorTableChangesRealTime();
await service.ShouldCallIncrementWithRevert();

// var numbers = new MySequenceOfNumbers();
// numbers.Subscribe(
//     number => Console.WriteLine($"Received value: {number}"),
//     () => Console.WriteLine("Sequence terminated"));