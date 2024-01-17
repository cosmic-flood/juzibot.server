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
        public void ReceiveMessageCallback([FromBody] dynamic body)
        {
            string bodyStr = body.ToString();
            Console.WriteLine(bodyStr);
            _logger.LogInformation(bodyStr);
        }


        [HttpGet]
        [Route("RetrieveArticleContent")]
        public async Task RetrieveArticleContent()
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "C# console program");

            var content = await client.GetStringAsync("http://mp.weixin.qq.com/s?__biz=MjM5MzE3NzE1OA==&mid=2247508851&idx=1&sn=906ff32495cafe12bdc59cc0ee0ee576&chksm=a699e85a91ee614cb5e793bd39f434e26bb1f13b5660d26162815dbaa0d3aae42e7e515ed8cc&mpshare=1&scene=1&srcid=0114zTvWnKU68Z7OiOwzykP9&sharer_shareinfo=ef4e7acd1e73a0e0987db471f8e8749b&sharer_shareinfo_first=81d2d798cd2d65f3253f4f97c5cd9047#rd");


            var startIndex = content.IndexOf("<blockquote class=\"js_blockquote_wrap\"");
            var startStr = content.Substring(startIndex);
            var endIndex = startStr.IndexOf("</blockquote");
            var finalStr = startStr.Substring(0, endIndex);
            Console.WriteLine(finalStr);
            _logger.LogInformation(finalStr);
        }

    }

}
