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
        [HttpPost]
        [Route("ReceiveMessageCallback")]
        public void ReceiveMessageCallback([FromBody] dynamic body)
        {
            Console.WriteLine(body.ToString());
        }
    }

}
