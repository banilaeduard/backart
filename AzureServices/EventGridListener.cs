using Azure.Messaging.ServiceBus;
using AzureServices;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

public class EventGridListener<T> : IAsyncDisposable
{
    private Func<T, CancellationToken, Task> _action = null;
    private readonly ServiceBusClient _client;
    private readonly ServiceBusProcessor _processor;
    private readonly CancellationToken _userToken;
    private readonly ILogger _logger;

    public EventGridListener(string connectionString, string queueName, Func<T, CancellationToken, Task> action, CancellationToken userToken, ILogger logger)
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
        _userToken = userToken;
        _logger = logger;
    }

    public async Task StartAsync()
    {
        await _processor.StartProcessingAsync();
        _logger.LogInformation("Listening for Event Grid messages...");
    }

    public async Task StopAsync()
    {
        await _processor.StopProcessingAsync();
        await _processor.DisposeAsync();
        await _client.DisposeAsync();
    }

    private async Task ProcessMessageHandler(ProcessMessageEventArgs args)
    {
        var token = CancellationTokenSource.CreateLinkedTokenSource(_userToken, args.CancellationToken).Token;

        // TODO: Deserialize EventGridEvent
        if (_action != null)
        {
            var body = args.Message.Body.ToObjectFromJson<EventGridMessage<T>>();
            await _action(body.data, token);
        }
        if (!token.IsCancellationRequested)
        {
            await args.CompleteMessageAsync(args.Message);
        }
    }

    private Task ErrorHandler(ProcessErrorEventArgs args)
    {
        _logger.LogError(new EventId(44), args.Exception, $@"{args.ErrorSource}");
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        _logger.LogInformation("Stopping Event Grid listener...");

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
