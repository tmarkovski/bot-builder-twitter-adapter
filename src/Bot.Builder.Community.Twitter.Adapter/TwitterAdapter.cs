using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bot.Builder.Community.Twitter.Webhooks.Authentication;
using Bot.Builder.Community.Twitter.Webhooks.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Options;

namespace Bot.Builder.Community.Twitter.Adapter
{
    public class TwitterAdapter : BotAdapter
    {
        private DirectMessageSender _sender;

        public TwitterAdapter(IOptions<TwitterAuthContext> options)
        {
            _sender = new DirectMessageSender(options.Value);
        }

        public override async Task<ResourceResponse[]> SendActivitiesAsync(ITurnContext turnContext, Activity[] activities, CancellationToken cancellationToken)
        {
            var responses = new List<ResourceResponse>();
            foreach (var activity in activities)
            {
                await _sender.SendAsync(long.Parse(activity.Recipient.Id), activity.Text);
                responses.Add(new ResourceResponse(activity.Id));
            }
            return responses.ToArray();
        }

        public override Task<ResourceResponse> UpdateActivityAsync(ITurnContext turnContext, Activity activity, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public override Task DeleteActivityAsync(ITurnContext turnContext, ConversationReference reference,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
