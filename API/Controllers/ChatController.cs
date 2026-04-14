using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        // memorija za chat – u produkciji može baza
        private static readonly List<ChatMessage> _messages = new();

        // POST api/chat/send
        [HttpPost("send")]
        public IActionResult SendMessage([FromBody] ChatMessage message)
        {
            if (string.IsNullOrWhiteSpace(message.ClientId) || string.IsNullOrWhiteSpace(message.Text))
                return BadRequest("ClientId or Text missing");

            // dodajemo poruku u listu
            _messages.Add(message);

            // vraćamo sve poruke (ili poslednjih n, po želji)
            return Ok(_messages);
        }

        // GET api/chat/history
        [HttpGet("history")]
        public IActionResult GetHistory()
        {
            return Ok(_messages);
        }
    }

    public class ChatMessage
    {
        public string ClientId { get; set; } = "";
        public string Text { get; set; } = "";
    }
}
