using AnyService.SampleApp.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnyService.SampleApp.Controllers
{
    [Route("notify")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly IHubContext<ChatHub> _chatHub;

        public NotificationController(IHubContext<ChatHub> chatHub)
        {
            _chatHub = chatHub;
        }
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] string payload)
        {
            await _chatHub.Clients.All.SendAsync("ReceiveMessage", payload);
            return NoContent();
        }
    }
}