using AIChatService.DTOs;
using AIChatService.Services;
using Microsoft.AspNetCore.Mvc;

namespace AIChatService.Controllers
{
    [ApiController]
    [Route("api/chat")]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private static readonly Guid MockUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost("sessions")]
        public async Task<ActionResult<SessionResponse>> CreateSession(CreateSessionRequest request)
        {
            var result = await _chatService.CreateSessionAsync(MockUserId, request);
            return Ok(result);
        }

        [HttpGet("sessions")]
        public async Task<ActionResult<IEnumerable<SessionResponse>>> GetSessions()
        {
            var result = await _chatService.GetUserSessionsAsync(MockUserId);
            return Ok(result);
        }

        [HttpGet("sessions/{sessionId}/messages")]
        public async Task<ActionResult<IEnumerable<MessageResponse>>> GetMessages(Guid sessionId)
        {
            var result = await _chatService.GetSessionMessagesAsync(sessionId);
            return Ok(result);
        }

        [HttpPost("ask")]
        public async Task<ActionResult<ChatResponse>> Ask(ChatRequest request)
        {
            try
            {
                var result = await _chatService.AskAiAsync(MockUserId, request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpDelete("sessions/{sessionId}")]
        public async Task<IActionResult> DeleteSession(Guid sessionId)
        {
            var success = await _chatService.DeleteSessionAsync(MockUserId, sessionId);
            if (!success) return NotFound();

            return NoContent();
        }
    }
}
