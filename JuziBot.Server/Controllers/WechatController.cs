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
        public async Task<string> ReceiveMessageCallback([FromBody]WechatMessageModel body)
        {
            string bodyStr = System.Text.Json.JsonSerializer.Serialize(body);
            _logger.LogInformation(bodyStr);
            if(!string.IsNullOrWhiteSpace(body.payload.text) && 
                body.payload.text.Trim().StartsWith("http://mp.weixin.qq.com/"))
            {
                var url = body.payload.text.Trim().Replace("\\u0026", "&").Replace(" ", "");
                string htmlContent = await GetArticleContentAsync(url);
                
                var summary = await CallOpenAIAsync(htmlContent);
                _logger.LogInformation("Send Summary To Wechat User");
                await SendSummaryToWechatAsync(summary, body);
                return summary;
            }
            return "遇到内部错误，未能总结";
        }


        [HttpGet]
        [Route("RetrieveArticleContent")]
        public async Task RetrieveArticleContent()
        {
            string finalStr = await GetArticleContentAsync("http://mp.weixin.qq.com/s?__biz=MjM5MzE3NzE1OA==&mid=2247508851&idx=1&sn=906ff32495cafe12bdc59cc0ee0ee576&chksm=a699e85a91ee614cb5e793bd39f434e26bb1f13b5660d26162815dbaa0d3aae42e7e515ed8cc&mpshare=1&scene=1&srcid=0114zTvWnKU68Z7OiOwzykP9&sharer_shareinfo=ef4e7acd1e73a0e0987db471f8e8749b&sharer_shareinfo_first=81d2d798cd2d65f3253f4f97c5cd9047#rd");
            Console.WriteLine(finalStr);
            _logger.LogInformation(finalStr);
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

            string[] symboles = ["div", "p", "blockquote", "section", "span", "img", "ul", "li", "strong", "br", "em"];
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
                                if (symbole == "li")
                                {
                                    finalStr = finalStr.Insert(tS, " \n\r - ");
                                    tS = tS + 6;
                                    tE = tE + 8;
                                }
                                finalStr = finalStr.Remove(tS, tE + 1);
                            }
                            //var tES = finalStr.IndexOf($"</{symbole}>");
                            //if (tES > 0)
                            //    finalStr = finalStr.Remove(tES, symbole.Length + 3);
                        }
                        catch (Exception exp)
                        {
                            _logger.LogError(exp.Message);
                            return "遇到内部错误，未能总结";
                        }
                    }
                    else
                    {
                        finalStr = finalStr.Replace($"</{symbole}>", "");
                        finalStr = finalStr.Replace($"<{symbole}/>", "");
                        finalStr = finalStr.Replace($"<{symbole} />", "");
                        //finalStr = finalStr.Replace($"\n\r", " ");
                        break;
                    }
                }
            }

            return finalStr;
        }

        private async Task<string> CallOpenAIAsync(string prompt)
        {
            _openAIService.SetDefaultModelId(Models.Gpt_3_5_Turbo_16k);
            var completionResult = await _openAIService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
            {
                Messages = new List<ChatMessage>
                {
                    ChatMessage.FromSystem("微信公众号文章分析总结"),
                    ChatMessage.FromUser($"```html  \n\r {prompt}  \n\r ```  请帮我对上面Html中的文章内容进行总结，并将总结限制在180字以内")
                },
                Model = Models.Gpt_3_5_Turbo_16k,
                MaxTokens = 10000//optional
            });
            if (completionResult.Successful)
            {
                var summary = completionResult.Choices.First().Message.Content;
                _logger.LogInformation(summary);
                return summary ?? "";
            }
            return "遇到内部错误，未能总结";
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
