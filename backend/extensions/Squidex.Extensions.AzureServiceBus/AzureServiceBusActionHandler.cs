// file:	Squidex.Extensions.AzureServiceBus\AzureServiceBusActionHandler.cs
//
// summary:	Implements the azure service bus action handler class
using Azure;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Squidex.Domain.Apps.Core.HandleRules;
using Squidex.Domain.Apps.Core.Rules.EnrichedEvents;
using System.Text;

namespace Squidex.Extensions.AzureServiceBus;

/// <summary>
/// An azure service bus action handler.
/// </summary>
public class AzureServiceBusActionHandler : RuleActionHandler<AzureServiceBusAction, AzureServiceBusJob>
{
    /// <summary>
    /// Options for controlling the client.
    /// </summary>
    private static ServiceBusClientOptions _clientOptions = new ServiceBusClientOptions
    {
        TransportType = ServiceBusTransportType.AmqpWebSockets
    };

    /// <summary>
    /// (Immutable) the logger.
    /// </summary>
    private readonly ILogger<AzureServiceBusActionHandler> _logger;

    /// <summary>
    /// (Immutable) the clients.
    /// </summary>
    private readonly ServiceBusClientPool<(string Hostname, string TopicName, string? AccessKey, string? AccessKeyName), AzureServiceObjectCache> clients;

    /// <summary>
    /// The cached credential.
    /// </summary>
    private DefaultAzureCredential? _cachedCredential;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="formatter"> The formatter.</param>
    /// <param name="logger"> The logger.</param>
    public AzureServiceBusActionHandler(RuleEventFormatter formatter, ILogger<AzureServiceBusActionHandler> logger)
       : base(formatter)
    {
        clients = new ServiceBusClientPool<(string Hostname, string TopicName, string? AccessKey, string? AccessKeyName), AzureServiceObjectCache>(async (key) =>
        {
            ServiceBusClient client;
            if (string.IsNullOrEmpty(key.AccessKey) || string.IsNullOrEmpty(key.AccessKeyName))
            {
                if (_cachedCredential == null)
                {
                    _cachedCredential = new();
                }

                client = new ServiceBusClient(key.Hostname, _cachedCredential, _clientOptions);
            }
            else
            {
                client = new ServiceBusClient(key.Hostname, new AzureNamedKeyCredential(key.AccessKeyName, key.AccessKey), _clientOptions);
            }
            var sender = client.CreateSender(key.TopicName, new ServiceBusSenderOptions { Identifier = "Squidex" });

            return new AzureServiceObjectCache
            {
                Client = client,
                Sender = sender
            };
        }, ClientEvicted);
        _logger = logger;
    }

    /// <summary>
    /// Creates job asynchronous.
    /// </summary>
    /// <param name="event"> The event.</param>
    /// <param name="action"> The action.</param>
    /// <returns>
    /// The create job.
    /// </returns>
    protected override async Task<(string Description, AzureServiceBusJob Data)> CreateJobAsync(EnrichedEvent @event, AzureServiceBusAction action)
    {
        var topicName = await FormatAsync(action.TopicName, @event);
        var hostName = await FormatAsync(action.Hostname, @event);

        string? requestBody;

        if (!string.IsNullOrEmpty(action.Payload))
        {
            requestBody = await FormatAsync(action.Payload, @event);
        }
        else
        {
            requestBody = ToEnvelopeJson(@event);
        }
        var correlationId = Guid.NewGuid().ToString("N");

        var ruleText = $"Send AzureServiceBusJob to topic '{topicName}' - {correlationId}";
        var ruleJob = new AzureServiceBusJob
        {
            CorrelationId = correlationId,
            Hostname = hostName!,
            TopicName = topicName!,
            AccessKey = action.AccessKey,
            AccessKeyName = action.AceessKeyName,
            MessageBodyV2 = requestBody
        };

        return (ruleText, ruleJob);
    }

    /// <summary>
    /// Executes the 'job asynchronous' operation.
    /// </summary>
    /// <param name="job"> The job.</param>
    /// <param name="ct"> (Optional) A token that allows processing to be cancelled.</param>
    /// <returns>
    /// The execute job.
    /// </returns>
    protected override async Task<Result> ExecuteJobAsync(AzureServiceBusJob job, CancellationToken ct = default)
    {
        _logger.LogDebug("Sending Azure Service Bus message to {Hostname}/{TopicName}", job.Hostname, job.TopicName);
        if (string.IsNullOrEmpty(job.MessageBodyV2))
        {
            return Result.Complete();
        }

        var queue = await clients.GetClientAsync((job.Hostname, job.TopicName, job.AccessKey, job.AccessKeyName));
        try
        {
            var batch = await queue.Sender.CreateMessageBatchAsync(CancellationToken.None);

            var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(job.MessageBodyV2));
            message.ContentType = "application/json";
            message.CorrelationId = job.CorrelationId;
            batch.TryAddMessage(message);

            await queue.Sender.SendMessagesAsync(batch, CancellationToken.None);
            return Result.Complete();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to Azure Service Bus");
            return Result.Failed(ex);
        }
    }

    /// <summary>
    /// Client evicted.
    /// </summary>
    /// <param name="pool"> The pool.</param>
    /// <param name="key"> The key.</param>
    /// <param name="client"> The client.</param>
    /// <returns>
    /// A Task.
    /// </returns>
    private async Task ClientEvicted(ServiceBusClientPool<(string Hostname, string TopicName, string? AccessKey, string? AccessKeyName),
        AzureServiceObjectCache> pool,
        (string Hostname, string TopicName, string? AccessKey, string? AccessKeyName) key,
        AzureServiceObjectCache client)
    {
        _logger.LogDebug("Evicting Azure Service Bus client {Hostname}/{TopicName}", key.Hostname, key.TopicName);
        await client.Sender.DisposeAsync();
        await client.Client.DisposeAsync();
    }
}

/// <summary>
/// An azure service object cache. This class cannot be inherited.
/// </summary>
internal sealed class AzureServiceObjectCache
{
    /// <summary>
    /// Gets or sets the client.
    /// </summary>
    /// <value>
    /// The client.
    /// </value>
    public ServiceBusClient Client { get; set; } = default!;

    /// <summary>
    /// Gets or sets the sender.
    /// </summary>
    /// <value>
    /// The sender.
    /// </value>
    public ServiceBusSender Sender { get; set; } = default!;
}

/// <summary>
/// An azure service bus job. This class cannot be inherited.
/// </summary>
public sealed class AzureServiceBusJob
{
    /// <summary>
    /// Gets or sets the access key.
    /// </summary>
    /// <value>
    /// The access key.
    /// </value>
    public string? AccessKey { get; set; }

    /// <summary>
    /// Gets or sets the name of the access key.
    /// </summary>
    /// <value>
    /// The name of the access key.
    /// </value>
    public string? AccessKeyName { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the correlation.
    /// </summary>
    /// <value>
    /// The identifier of the correlation.
    /// </value>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the hostname.
    /// </summary>
    /// <value>
    /// The hostname.
    /// </value>
    public string Hostname { get; set; }

    /// <summary>
    /// Gets or sets the message body v 2.
    /// </summary>
    /// <value>
    /// The message body v 2.
    /// </value>
    public string? MessageBodyV2 { get; set; }

    /// <summary>
    /// Gets or sets the name of the topic.
    /// </summary>
    /// <value>
    /// The name of the topic.
    /// </value>
    public string TopicName { get; set; }
}
