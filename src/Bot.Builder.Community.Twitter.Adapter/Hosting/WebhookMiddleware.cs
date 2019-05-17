using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Bot.Builder.Community.Twitter.Webhooks.Authentication;
using Bot.Builder.Community.Twitter.Webhooks.Models;
using Bot.Builder.Community.Twitter.Webhooks.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IMiddleware = Microsoft.AspNetCore.Http.IMiddleware;

namespace Bot.Builder.Community.Twitter.Adapter.Hosting
{
    public class WebhookMiddleware : IMiddleware
    {
        private readonly ILogger<WebhookMiddleware> _logger;
        private readonly IBot _bot;
        private readonly TwitterAdapter _adapter;
        private readonly TwitterAuthContext _context;
        private readonly WebhookInterceptor _interceptor;
        private readonly DirectMessageSender _sender;

        public WebhookMiddleware(IOptions<TwitterAuthContext> context, ILogger<WebhookMiddleware> logger, IBot bot, TwitterAdapter adapter)
        {
            _logger = logger;
            _bot = bot;
            _adapter = adapter;
            _context = context.Value;
            _interceptor = new WebhookInterceptor(_context.ConsumerSecret);
            _sender = new DirectMessageSender(_context);
        }
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var result = await _interceptor.InterceptIncomingRequest(context.Request, OnDirectMessageReceived);
            if (result.IsHandled)
            {
                context.Response.StatusCode = (int) HttpStatusCode.OK;
                await context.Response.WriteAsync(result.Response);
            }
            else
            {
                _logger.LogError($"Failed to intercept message.");
                await next(context);
            }
        }

        private async void OnDirectMessageReceived(DirectMessageEvent obj)
        {
            _logger.LogInformation($"Direct Message from {obj.Sender.Name} (@{obj.Sender.ScreenName}): {obj.MessageText}");

            if (obj.Sender.ScreenName != _context.BotUsername)
            {
                // Only respond if sender is different than the bot
                await _bot.OnTurnAsync(new TurnContext(_adapter, new Activity
                {
                    Text = obj.MessageText,
                    Type = "message",
                    From = new ChannelAccount(obj.Sender.Id, obj.Sender.ScreenName),
                    Recipient = new ChannelAccount(obj.Recipient.Id, obj.Recipient.ScreenName)
                }));
            }
        }
    }
}
