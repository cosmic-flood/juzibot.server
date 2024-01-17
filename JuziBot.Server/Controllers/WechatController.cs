using JuziBot.Server.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        public WechatController(ILogger<WechatController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        [Route("ReceiveMessageCallback")]
        public async Task ReceiveMessageCallback([FromBody]WechatMessageModel body)
        {
            string bodyStr = JsonSerializer.Serialize(body);
            _logger.LogInformation(bodyStr);
            if(!string.IsNullOrWhiteSpace(body.payload.text) && 
                body.payload.text.Trim().StartsWith("http://mp.weixin.qq.com/"))
            {
                var url = body.payload.text.Trim().Replace("\\u0026", "&").Replace(" ", "");
                string htmlContent = await GetArticleContentAsync(url);
            }
        }


        [HttpGet]
        [Route("RetrieveArticleContent")]
        public async Task RetrieveArticleContent()
        {
            string finalStr = await GetArticleContentAsync("http://mp.weixin.qq.com/s?__biz=MjM5MzE3NzE1OA==&mid=2247508851&idx=1&sn=906ff32495cafe12bdc59cc0ee0ee576&chksm=a699e85a91ee614cb5e793bd39f434e26bb1f13b5660d26162815dbaa0d3aae42e7e515ed8cc&mpshare=1&scene=1&srcid=0114zTvWnKU68Z7OiOwzykP9&sharer_shareinfo=ef4e7acd1e73a0e0987db471f8e8749b&sharer_shareinfo_first=81d2d798cd2d65f3253f4f97c5cd9047#rd");
            Console.WriteLine(finalStr);
            _logger.LogInformation(finalStr);
        }

        private async Task<string> GetArticleContentAsync(string url)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Yo Young Fella");

            var content = await client.GetStringAsync(url);
            var startIndex = content.IndexOf("id=\"js_content\"");
            var startStr = content.Substring(startIndex);
            var endIndex = startStr.IndexOf("<script type=\"text/javascript\"");
            var finalStr = startStr.Substring(0, endIndex);
            return finalStr;
        }

    }

}
