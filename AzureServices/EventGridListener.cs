using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;

public class EventGridListener<T> : IAsyncDisposable
{
    private Func<T, CancellationToken, Task> _action = null;
    private readonly ServiceBusClient _client;
    private readonly ServiceBusProcessor _processor;

    public EventGridListener(string connectionString, string queueName, Func<T, CancellationToken, Task> action)
    {
        _client = new ServiceBusClient(connectionString);
        _processor = _client.CreateProcessor(queueName, new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = 1,
            AutoCompleteMessages = false
        });

        _processor.ProcessMessageAsync += ProcessMessageHandler;
        _processor.ProcessErrorAsync += ErrorHandler;
        _action = action;
    }

    public async Task StartAsync()
    {
        await _processor.StartProcessingAsync();
        Console.WriteLine("Listening for Event Grid messages...");
    }

    public async Task StopAsync()
    {
        await _processor.StopProcessingAsync();
        await _processor.DisposeAsync();
        await _client.DisposeAsync();
    }

    private async Task ProcessMessageHandler(ProcessMessageEventArgs args)
    {
        var body = args.Message.Body.ToString();
        Console.WriteLine($"Received Event Grid message: {body}");

        // TODO: Deserialize EventGridEvent
        if (_action != null)
        {
            var eventGridEvent = JsonConvert.DeserializeObject<dynamic>(body)!;
            await _action(eventGridEvent.data, args.CancellationToken);
        }
        if (!args.CancellationToken.IsCancellationRequested)
        {
            await args.CompleteMessageAsync(args.Message);
        }
    }

    private Task ErrorHandler(ProcessErrorEventArgs args)
    {
        Console.WriteLine($"Error: {args.Exception}");
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        Console.WriteLine("Stopping Event Grid listener...");

        if (_processor != null)
        {
            await _processor.StopProcessingAsync();
            await _processor.DisposeAsync();
        }

        if (_client != null)
        {
            await _client.DisposeAsync();
        }
    }
}
