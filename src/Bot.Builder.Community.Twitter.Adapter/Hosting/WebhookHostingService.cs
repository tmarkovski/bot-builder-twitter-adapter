using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bot.Builder.Community.Twitter.Webhooks.Authentication;
using Bot.Builder.Community.Twitter.Webhooks.Models.Twitter;
using Bot.Builder.Community.Twitter.Webhooks.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tweetinvi.Core.Controllers;
using Tweetinvi.Models;

namespace Bot.Builder.Community.Twitter.Adapter.Hosting
{        
    /// <inheritdoc />
    /// <summary>
    /// Webhook Hosted Service
    /// </summary>
    public class WebhookHostedService : IHostedService
    {
        private readonly ILogger<WebhookHostedService> _logger;
        private readonly IWebhookController _webhookController;
        private readonly TwitterAuthContext _authContext;
        private readonly WebhooksPremiumManager _webhooksManager;
        private readonly SubscriptionsManager _subscriptionsManager;

        public WebhookHostedService(
            IApplicationLifetime applicationLifetime,
            IOptions<TwitterAuthContext> authContext,
            ILogger<WebhookHostedService> logger,
            IWebhookController webhookController)
        {
            _logger = logger;
            _webhookController = webhookController;
            _authContext = authContext.Value;
            _webhooksManager = new WebhooksPremiumManager(_authContext);
            _subscriptionsManager = new SubscriptionsManager(_authContext);

            // Initialize logic after host has started, to ensure WebhookMiddleware
            // is available for webhook registration
            applicationLifetime.ApplicationStarted.Register(InitializeWebhookAsync);
        }

        public ITwitterCredentials GetUserCredentials() =>
            new TwitterCredentials(
                _authContext.ConsumerKey, 
                _authContext.ConsumerSecret,
                _authContext.AccessToken, 
                _authContext.AccessSecret);

        private async void InitializeWebhookAsync()
        {
            if (_authContext.Tier != TwitterAccountApi.PremiumFree)
            {
                throw new NotSupportedException($"{_authContext.Tier} tier not yet supported");
            }

            var webhooks = await _webhooksManager.GetRegisteredWebhooks();
            if (webhooks.Success)
            {
                if (webhooks.Data.Environments.FirstOrDefault(x => x.Name == _authContext.Environment) is EnvironmentRegistration environmentRegistration
                    && environmentRegistration.Webhooks.FirstOrDefault() is WebhookRegistration webhookRegistration)
                {
                    if (webhookRegistration.RegisteredUrl == _authContext.WebhookUri)
                    {
                        if (webhookRegistration.IsValid)
                        {
                            // Webhook registered and valid.
                            _logger.LogInformation("Found valid webhook {WebHook} for environment {Environment}", 
                                _authContext.WebhookUri, _authContext.Environment);
                        }
                        else
                        {
                            _logger.LogWarning("Found invalid webhook {WebHook} for environment {Environment}. Attempting to update....", 
                                _authContext.WebhookUri, _authContext.Environment);
                            // Call update webhook to initiate CRC 
                        }
                    }
                    else
                    {
                        _logger.LogInformation($"Found webhook '{webhookRegistration.RegisteredUrl}', but configured uri is '{_authContext.WebhookUri}' " +
                                         $"for environment '{_authContext.Environment}'. Attempting to update...");

                        var removeResult = await _webhooksManager.UnregisterWebhook(webhookRegistration.Id, _authContext.Environment);

                        if (!removeResult.Success)
                        {
                            _logger.LogError("Failed to remove old webhook.");
                            return;
                        }

                        // Webhook Url is different than current one. Register new webhook.
                        // This will override the webhook in PremiumFree tier, as only one webhook per environment is allowed
                        var result = await _webhooksManager.RegisterWebhook(_authContext.WebhookUri, _authContext.Environment);
                        if (result.Success)
                        {
                            _logger.LogInformation($"Webhook registration initiated");
                        }
                        else
                        {
                            _logger.LogError($"Webhook registration error: {string.Join(", ", result.Error.Errors.Select(x => x.Message))}");
                        }
                    }
                }
                else
                {
                    _logger.LogInformation($"Webhook not found. Registering [{_authContext.WebhookUri}] for [{_authContext.Environment}]");

                    await _webhooksManager.RegisterWebhook(_authContext.WebhookUri, _authContext.Environment);
                }

                // Check subscription
                var checkSubResult = await _subscriptionsManager.CheckSubscription(_authContext.Environment);
                if (checkSubResult.Success)
                {
                    if (checkSubResult.Data)
                    {
                        _logger.LogInformation("Found valid subscription");
                    }
                    else
                    {
                        var subResult = await _subscriptionsManager.Subscribe(_authContext.Environment);
                        if (subResult.Success)
                        {
                            _logger.LogInformation("Subscription registration completed");
                        }
                        else
                        {
                            _logger.LogError("Failed to register subscription: {Error}", 
                                string.Join(", ", subResult.Error.Errors.Select(x => $"{x.Code}: {x.Message}")));
                        }
                    }
                }
                else
                {
                    _logger.LogError("Failed to check subscription: {Error}", 
                        string.Join(", ", checkSubResult.Error.Errors.Select(x => $"{x.Code}: {x.Message}")));
                }
            }
        }

        /// <inheritdoc />
        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        /// <inheritdoc />
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
