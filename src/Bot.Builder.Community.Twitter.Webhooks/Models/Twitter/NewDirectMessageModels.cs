using Newtonsoft.Json;

namespace Bot.Builder.Community.Twitter.Webhooks.Models.Twitter
{

    public class NewDirectMessageObject
    {
        [JsonProperty("event")]
        public Event Event { get; set; }
    }
      
    public class Event
    {
        [JsonProperty("type")]
        public string EventType { get; set; }

        [JsonProperty("message_create")]
        public NewEvent_MessageCreate MessageCreate { get; set; }
    }

    public class NewEvent_MessageCreate
    {
        public Target target { get; set; }
        public NewEvent_MessageData message_data { get; set; }
    }
 
    public class NewEvent_MessageData
    {
        [JsonProperty("text")]
        public string Text { get; set; }
    }


}
