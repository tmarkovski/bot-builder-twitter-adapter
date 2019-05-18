using System;
using System.Collections.Generic;
using System.Text;
using Bot.Builder.Community.Twitter.Adapter.Hosting;
using Bot.Builder.Community.Twitter.Webhooks.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Tweetinvi;
using Tweetinvi.AspNet;
using Tweetinvi.Core.Public.Models.Authentication;
using WebhookMiddleware = Bot.Builder.Community.Twitter.Adapter.Hosting.WebhookMiddleware;

namespace Bot.Builder.Community.Twitter.Adapter
{
    public static class ServiceCollectionExtensions
    {
        public static void AddTwitterAdapter(this IServiceCollection collection, Action<TwitterAuthContext> contextDelegate)
        {
            collection.AddSingleton<IHostedService, WebhookHostedService>();
            collection.AddSingleton<WebhookMiddleware>();
            collection.AddSingleton<TwitterAdapter>();
            
            Plugins.Add<WebhooksPlugin>();
            
            collection.AddSingleton(services =>
            {
                var context = services.GetRequiredService<IOptions<TwitterAuthContext>>().Value;
                var consumerOnlyCredentials = new ConsumerOnlyCredentials(context.ConsumerKey, context.ConsumerSecret)
                {
                    ApplicationOnlyBearerToken = "BEARER_TOKEN"
                };
                return new WebhookConfiguration(consumerOnlyCredentials);
            });

            collection.AddOptions();
            collection.Configure(contextDelegate);
        }
    }

    public static class ApplicationBuilderExtensions
    {
        public static void UseTwitterAdapter(this IApplicationBuilder app)
        {
            //var twitterOptions = app.ApplicationServices.GetRequiredService<IOptions<TwitterAuthContext>>().Value;
            //var uriPath = new Uri(twitterOptions.WebhookUri);
            
            app.UseTweetinviWebhooks(app.ApplicationServices.GetRequiredService<WebhookConfiguration>());

            //app.UseWhen(
            //    context => context.Request.Path.StartsWithSegments(uriPath.AbsolutePath), 
            //    builder => builder.UseMiddleware<WebhookMiddleware>());
        }
    }
}
