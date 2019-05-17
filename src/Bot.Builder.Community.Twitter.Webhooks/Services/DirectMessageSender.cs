using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Bot.Builder.Community.Twitter.Webhooks.Authentication;
using Bot.Builder.Community.Twitter.Webhooks.Models;
using Bot.Builder.Community.Twitter.Webhooks.Models.Twitter;
using Newtonsoft.Json;

namespace Bot.Builder.Community.Twitter.Webhooks.Services
{

    /// <summary>
    /// Helper class to send Direct Message to twitter user using the screen name.
    /// </summary>
    public class DirectMessageSender
    {
        public TwitterAuthContext AuthContext { get; set; }

        public DirectMessageSender(TwitterAuthContext context)
        {
            AuthContext = context;
        }


        /// <summary>
        /// Send a direct message to User from the current user (using AuthContext).
        /// </summary>
        /// <param name="toScreenName">To (screen name without '@' sign)</param>
        /// <param name="messageText">Message Text to send.. Less than 140 char.</param>
        /// <returns>
        /// </returns>
        [Obsolete("Use SendAsync instead.")]
        public async Task<Result<MessageCreate>> Send(string toScreenName, string messageText)
        {

            //TODO: Provide a generic class to make Twitter API Requests.

            if (string.IsNullOrEmpty(messageText))
            {
                throw new TwitterException("You can't send an empty message.");
            }

            if (messageText.Length > 140)
            {
                throw new TwitterException(
                    "You can't send more than 140 char using this end point, use SendAsync instead.");
            }

            messageText = Uri.EscapeDataString(messageText);
            var resourceUrl =
                $"https://api.twitter.com/1.1/direct_messages/new.json?Text={messageText}&screen_name={toScreenName}";

            HttpResponseMessage response;
            using (var client = new HttpClient())
            {

                client.DefaultRequestHeaders.Add("Authorization",
                    AuthHeaderBuilder.Build(AuthContext, HttpMethod.Post, resourceUrl));

                response = await client.PostAsync(resourceUrl, new StringContent(""));
            }

            if (response.StatusCode == HttpStatusCode.OK)
            {

                var msgCreateJson = await response.Content.ReadAsStringAsync();
                var mCreateObj = JsonConvert.DeserializeObject<MessageCreate>(msgCreateJson);
                return new Result<MessageCreate>(mCreateObj);
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();

            if (!string.IsNullOrEmpty(jsonResponse))
            {
                var err = JsonConvert.DeserializeObject<TwitterError>(jsonResponse);
                return new Result<MessageCreate>(err);
            }

            return new Result<MessageCreate>();
        }


        /// <summary>
        /// Send a direct message to a user by userId, from the current user (using AuthContext).
        /// </summary>
        /// <param name="userId">The Twitter User Id to send the message to.</param>
        /// <param name="messageText">The Text of the message, should be less than 10,000 chars.</param>
        /// <returns></returns>
        public async Task<Result<DirectMessageResult>> SendAsync(long userId, string messageText)
        {

            //TODO: Provide a generic class to make Twitter API Requests.

            if (string.IsNullOrEmpty(messageText))
            {
                throw new TwitterException("You can't send an empty message.");
            }

            if (messageText.Length > 10000)
            {
                throw new TwitterException(
                    "Invalid message, the length of the message should be less than 10000 chars.");
            }

            if (userId == default(long))
            {
                throw new TwitterException("Invalid userId.");
            }

            var resourceUrl = $"https://api.twitter.com/1.1/direct_messages/events/new.json";

            var newDmEvent = new NewDirectMessageObject
            {
                Event = new Event
                {
                    EventType = "message_create",
                    MessageCreate = new NewEvent_MessageCreate
                    {
                        message_data = new NewEvent_MessageData {Text = messageText},
                        target = new Target {recipient_id = userId.ToString()}
                    }
                }
            };
            var jsonObj = JsonConvert.SerializeObject(newDmEvent);


            HttpResponseMessage response;
            using (var client = new HttpClient())
            {

                client.DefaultRequestHeaders.Add("Authorization",
                    AuthHeaderBuilder.Build(AuthContext, HttpMethod.Post, resourceUrl));


                response = await client.PostAsync(resourceUrl,
                    new StringContent(jsonObj, Encoding.UTF8, "application/json"));
            }

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var msgCreateJson = await response.Content.ReadAsStringAsync();
                var mCreateObj = JsonConvert.DeserializeObject<NewDmResult>(msgCreateJson);
                return new Result<DirectMessageResult>(mCreateObj.@event);
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();

            if (!string.IsNullOrEmpty(jsonResponse))
            {
                var err = JsonConvert.DeserializeObject<TwitterError>(jsonResponse);
                return new Result<DirectMessageResult>(err);
            }
            return new Result<DirectMessageResult>();
        }
    }
}