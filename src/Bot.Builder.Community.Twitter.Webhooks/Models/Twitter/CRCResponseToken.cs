using Newtonsoft.Json;

namespace Bot.Builder.Community.Twitter.Webhooks.Models.Twitter
{
    internal class CRCResponseToken
    {
        [JsonProperty("response_token")]
        public string Token { get; set; }
    }
}
