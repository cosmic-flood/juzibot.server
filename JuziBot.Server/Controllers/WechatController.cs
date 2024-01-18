using JuziBot.Server.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OpenAI.Interfaces;
using OpenAI.Managers;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;
using System.Dynamic;
using System.Net;
using System.Text.Json;

namespace JuziBot.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WechatController : ControllerBase
    {
        private readonly ILogger<WechatController> _logger;
        private readonly IOpenAIService _openAIService;

        public WechatController(ILogger<WechatController> logger, IOpenAIService openAIService)
        {
            _logger = logger;
            _openAIService = openAIService;
        }

        [HttpPost]
        [Route("ReceiveMessageCallback")]
        //public async Task<string> ReceiveMessageCallback([FromBody]WechatMessageModel body)
        public async Task<string> ReceiveMessageCallback([FromBody]dynamic dbody)
        {
            string dbodyStr = dbody.ToString();
            _logger.LogInformation(dbodyStr);
            //Console.WriteLine(dbodyStr);
            var body = JsonConvert.DeserializeObject<WechatMessageModel>(dbodyStr);
            string bodyStr = System.Text.Json.JsonSerializer.Serialize(body);
            //_logger.LogInformation(bodyStr);
            //Console.WriteLine(bodyStr);
            if (!string.IsNullOrWhiteSpace(body.payload.url) && 
                (
                 body.payload.url.Trim().StartsWith("http://mp.weixin.qq.com/") ||
                 body.payload.url.Trim().StartsWith("https://mp.weixin.qq.com/")
                ))
            {
                var url = body.payload.url.Trim().Replace("\\u0026", "&").Replace(" ", "");
                string htmlContent = await GetArticleContentAsync(url);
                
                var summary = await CallOpenAIAsync(htmlContent);
                //_logger.LogInformation("Send Summary To Wechat User");
                //Console.WriteLine("Send Summary To Wechat User");
                await SendSummaryToWechatAsync(summary, body);
                return summary;
            }
            return "遇到内部错误，未能总结";
        }


        [HttpGet]
        [Route("RetrieveArticleContent")]
        public async Task<string> RetrieveArticleContent()
        {
            string finalStr = await GetArticleContentAsync("https://mp.weixin.qq.com/s?__biz=MzIyMDA3MjMwNw%3D%3D&mid=2455852912&idx=1&sn=fb5ef33c2dac532198114317996ec804&chksm=8044676cb733ee7a898e8ffdf1cd39f8834d58efd1ecac73b347f37e125006406b6c700be172&mpshare=1&scene=1&srcid=0118iRC6h2rTNNevbgInXxZD&sharer_shareinfo=5ae48380cce443714d49a365aaa67f61&sharer_shareinfo_first=5ae48380cce443714d49a365aaa67f61&from=industrynews#rd");
            Console.WriteLine(finalStr);
            _logger.LogInformation(finalStr);
            return finalStr;
        }

        [HttpPost]
        [Route("MakeContentASummary")]
        public async Task<string> MakeContentASummary([FromBody] ArticleHtmlContentModel body)
        {
            return await CallOpenAIAsync(body.Body);
        }

        private async Task<string> GetArticleContentAsync(string url)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Yo Young Fella");

            var content = await client.GetStringAsync(url);
            var startIndex = content.IndexOf("id=\"js_content\"");
            var startStr = content.Substring(startIndex);
            var endIndex = startStr.IndexOf("<script type=\"text/javascript\"");
            var finalStr = "<div " + startStr.Substring(0, endIndex).Replace("  ", "");

            string[] symboles = ["div", "p", "blockquote", "section", "span", "table", "tbody", "img", "ul", "li", "strong", "br", "em", "a", "tr", "td", "mp-"];
            foreach(var symbole in symboles)
            {
                while (true)
                {
                    if (finalStr.Contains($"<{symbole}"))
                    {
                        try
                        {
                            var tS = finalStr.IndexOf($"<{symbole}");
                            var tE = finalStr.Substring(tS).IndexOf(">");
                            if (tE > 0)
                            {
                                finalStr = finalStr.Remove(tS, tE + 1);
                            }
                        }
                        catch (Exception exp)
                        {
                            _logger.LogError(exp.Message);
                            Console.WriteLine(exp.Message);
                            return "遇到内部错误，未能总结";
                        }
                    }
                    else
                    {
                        finalStr = finalStr.Replace($"</{symbole}>", "");
                        finalStr = finalStr.Replace($"<{symbole}/>", "");
                        finalStr = finalStr.Replace($"<{symbole} />", "");
                        if (finalStr.Contains("</mp-") && symbole == "mp-")
                        {
                            var tS = finalStr.IndexOf($"</{symbole}");
                            var tE = finalStr.Substring(tS).IndexOf(">");
                            if (tE > 0)
                            {
                                finalStr = finalStr.Remove(tS, tE + 1);
                            }
                        }
                        break;
                    }
                }
            }

            return finalStr;
        }

        private async Task<string> CallOpenAIAsync(string prompt)
        {
            var model = Models.Gpt_3_5_Turbo_16k;
            Console.WriteLine($"Length of Prompt: {prompt}");
            _logger.LogInformation($"Length of Prompt: {prompt}");
            if (prompt.Length > 8192)
            {
                model = Models.Gpt_4_1106_preview;
            }
            _openAIService.SetDefaultModelId(model);
            var completionResult = await _openAIService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
            {
                Messages = new List<ChatMessage>
                {
                    ChatMessage.FromSystem("微信公众号文章分析总结"),
                    ChatMessage.FromUser($"```html  \n\r {prompt}  \n\r ```  请帮我对上面Html中的文章内容用中文进行总结，并将总结限制在180字以内")
                },
                Model = model,
                MaxTokens = 4096, //optional
            });
            if (completionResult.Successful)
            {
                var summary = completionResult.Choices.First().Message.Content;
                //Console.WriteLine(summary);
                //_logger.LogInformation(summary);
                return summary ?? "";
            }
            return "遇到内部错误，未能总结。有可能是文章太长造成的。";
        }

        private async Task SendSummaryToWechatAsync(string summary, WechatMessageModel wmm)
        {
            var client = new HttpClient();

            // Create HttpRequestMessage
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://hub.juzibot.com/api/v2/message/send?token=058c38ca57b643c080da6d47232c83a7"), // Use the correct URI
                Headers =
                {
                    { HttpRequestHeader.UserAgent.ToString(), "Apifox/1.0.0 (https://apifox.com)" },
                    { HttpRequestHeader.ContentType.ToString(), "application/json" }
                },
                Content = new StringContent(JsonConvert.SerializeObject(new
                {
                    imBotId = wmm.imBotId,
                    imContactId = wmm.imContactId,
                    messageType = 7,
                    payload = new { text = summary }
                }), System.Text.Encoding.UTF8, "application/json")
            };

            // Send the request
            var response = await client.SendAsync(request);

            // Read the response
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation(responseContent);
        }
    }

}
