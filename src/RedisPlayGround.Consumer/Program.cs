// See https://aka.ms/new-console-template for more information
using StackExchange.Redis;

Dictionary<string, string> ParseResult(StreamEntry entry) => entry.Values.ToDictionary(x => x.Name.ToString(), x => x.Value.ToString());

var tokenSource = new CancellationTokenSource();
var token = tokenSource.Token;

var endpoints = new EndPointCollection();
endpoints.Add("localhost", 6379);

var muxer = ConnectionMultiplexer.Connect(new ConfigurationOptions() { EndPoints = endpoints, Password = "password123" });
var db = muxer.GetDatabase();

const string streamName = "telemetry";
const string groupName = "avg";
if (!(await db.KeyExistsAsync(streamName)) || (await db.StreamGroupInfoAsync(streamName)).All(x => x.Name != groupName))
{
    await db.StreamCreateConsumerGroupAsync(streamName, groupName, "0-0", true);
}

await Task.Run(async () =>
{
    var lastId = "0-0";
    var checkBacklog = true;
    while (!token.IsCancellationRequested)
    {
        var targetId = (checkBacklog) ? lastId : ">";

        var result = await db.StreamReadGroupAsync(streamName, groupName, "avg-1", targetId, 1);
        if (result.Any())
        {
            var firstResult = result.First();
            var dict = ParseResult(firstResult);
            Console.WriteLine($"Group read result: temp: {dict["temp"]}, time: {dict["time"]}");
            await db.StreamAcknowledgeAsync(streamName, groupName, firstResult.Id);

            lastId = firstResult.Id;
        }

        checkBacklog = result.Any();
        await Task.Delay(100);
    }
});

// Read Most Recent Message
// var readTask = Task.Run(async () =>
// {
//     while (!token.IsCancellationRequested)
//     {
//         var result = await db.StreamRangeAsync(streamName, "-", "+", 1, Order.Descending);
//         if (result.Any())
//         {
//             var dict = ParseResult(result.First());
//             Console.WriteLine(result.Count());
//             Console.WriteLine($"Read result: temp {dict["temp"]} time: {dict["time"]}");
//         }

//         await Task.Delay(1000);
//     }
// });