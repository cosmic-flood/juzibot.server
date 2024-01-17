namespace JuziBot.Server.ViewModels
{
    public class WechatMessageModel
    {
        public string orgId { get; set; }
        public string token { get; set; }
        public string botId { get; set; }
        public string imBotId { get; set; }
        public string botUserId { get; set; }
        public string chatId { get; set; }
        public string avatar { get; set; }
        public bool coworker { get; set; }
        public string imContactId { get; set; }
        public string externalUserId { get; set; }
        public string contactName { get; set; }
        public int contactType { get; set; }
        public string messageId { get; set; }
        public bool isSelf { get; set; }
        public string sendBy { get; set; }
        public int source { get; set; }
        public long timestamp { get; set; }
        public int messageType { get; set; }
        public WechatMessagePayload payload { get; set; }
    }

    public class WechatMessagePayload
    {
        public List<object> mention { get; set; }
        public string text { get; set; }
        public string url { get; set; }
        public string title { get; set; }
        public string description { get; set; }
    }
}
