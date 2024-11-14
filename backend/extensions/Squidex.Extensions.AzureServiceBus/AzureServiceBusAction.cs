// file:	Squidex.Extensions.AzureServiceBus\AzureServiceBusAction.cs
//
// summary:	Implements the azure service bus action class
using Squidex.Domain.Apps.Core.HandleRules;
using Squidex.Domain.Apps.Core.Rules;
using Squidex.Infrastructure.Validation;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Squidex.Extensions.AzureServiceBus;

/// <summary>
/// (Immutable) an azure service bus action. This record cannot be inherited.
/// </summary>
[RuleAction(
Title = "Azure Service Bus",
IconImage = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 32 32'><path d='M.011 16L0 6.248l12-1.63V16zM14 4.328L29.996 2v14H14zM30 18l-.004 14L14 29.75V18zM12 29.495L.01 27.851.009 18H12z'/></svg>",
IconColor = "#0d9bf9",
Display = "Send to an Azure Service Bus Topic",
Description = "Send an event to azure service bus topic.",
ReadMore = "https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-messaging-overview")]
public sealed record AzureServiceBusAction : RuleAction
{
    /// <summary>
    /// Gets or sets the hostname.
    /// </summary>
    /// <value>
    /// The hostname.
    /// </value>
    [LocalizedRequired]
    [Display(Name = "Hostname", Description = "The host name of the Azure Service Bus instance.")]
    [Editor(RuleFieldEditor.Text)]
    [Formattable]
    public string Hostname { get; set; }

    /// <summary>
    /// Gets or sets the topic.
    /// </summary>
    /// <value>
    /// The topic.
    /// </value>
    [LocalizedRequired]
    [Display(Name = "Topic", Description = "The name of the topic to send the message to.")]
    [Editor(RuleFieldEditor.Text)]
    [Formattable]
    public string TopicName { get; set; }

    /// <summary>
    /// Gets or sets the name of the aceess key.
    /// </summary>
    /// <value>
    /// The name of the aceess key.
    /// </value>
    [Display(Name = "Access Key Name", Description = "Enter the access key name for the service bus topic (leave blank to use managed identities).")]
    [Editor(RuleFieldEditor.Text)]
    public string? AceessKeyName { get; set; }

    /// <summary>
    /// Gets or sets the access key.
    /// </summary>
    /// <value>
    /// The access key.
    /// </value>
    [Display(Name = "Access Key", Description = "Enter the key for the service bus topic (leave blank to use managed identities).")]
    [Editor(RuleFieldEditor.Password)]
    public string? AccessKey { get; set; }


    /// <summary>
    /// Gets or sets the payload.
    /// </summary>
    /// <value>
    /// The payload.
    /// </value>
    [Display(Name = "Payload (Optional)", Description = "Leave it empty to use the full event as body.")]
    [Editor(RuleFieldEditor.TextArea)]
    [Formattable]
    public string? Payload { get; set; }

    /// <summary>
    /// Enumerates custom validate in this collection.
    /// </summary>
    /// <returns>
    /// An enumerator that allows foreach to be used to process custom validate in this collection.
    /// </returns>
    protected override IEnumerable<ValidationError> CustomValidate()
    {
        if (!string.IsNullOrWhiteSpace(TopicName) && !Regex.IsMatch(TopicName, "^[a-z][a-z0-9]{2,}(\\-[a-z0-9]+)*$"))
        {
            yield return new ValidationError("Topic must be valid azure topic name.", nameof(TopicName));
        }

        if((!string.IsNullOrEmpty(AccessKey) && string.IsNullOrEmpty(AceessKeyName) ) || (string.IsNullOrEmpty(AccessKey) && !string.IsNullOrEmpty(AceessKeyName)))
        {
            yield return new ValidationError("Access Key and Access Key Name must be both empty or both filled.", nameof(AccessKey));
        }


        if (string.IsNullOrEmpty(Hostname))
        {
            yield return new ValidationError("Hostname must not be empty.", nameof(Hostname));
        }
    }
}
