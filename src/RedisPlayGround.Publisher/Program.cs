// See https://aka.ms/new-console-template for more information
using StackExchange.Redis;

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
    var random = new Random();
    while (!token.IsCancellationRequested)
    {
        var temp = random.Next(50, 65);
        var time = DateTimeOffset.Now.ToUnixTimeSeconds();
        await db.StreamAddAsync(streamName,
            new NameValueEntry[]
            {
                new NameValueEntry("temp", temp),
                new NameValueEntry("time", time)
            },
            maxLength: 10
        );
        await Task.Delay(2000);
        Console.WriteLine($"Published message (temp: {temp}, time: {time})");
    }
});