using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UserService.DTOs;
using UserService.Services;

namespace UserService.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly IUserAppService _userService;

        public UsersController(IUserAppService userService)
        {
            _userService = userService;
        }

        // ── PROFILES ──────────────────────────────────────────
        /// <summary>GET /api/users/profile — Lấy thông tin Profile người dùng</summary>
        [HttpGet("profile")]
        public async Task<ActionResult<UserProfileDto>> GetProfile()
        {
            var userId = GetUserIdFromHeader();
            var profile = await _userService.GetProfileAsync(userId);
            if (profile == null) return NotFound(new { message = "Profile not found" });
            return Ok(profile);
        }

        /// <summary>PUT /api/users/profile — Cập nhật Profile</summary>
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UserProfileUpdateDto dto)
        {
            var userId = GetUserIdFromHeader();
            try
            {
                await _userService.UpdateProfileAsync(userId, dto);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ── BOOKMARKS ─────────────────────────────────────────
        /// <summary>GET /api/users/bookmarks — Danh sách bookmark (filter theo entity_type)</summary>
        [HttpGet("bookmarks")]
        public async Task<ActionResult<IEnumerable<BookmarkDto>>> GetBookmarks([FromQuery] string? entity_type)
        {
            var userId = GetUserIdFromHeader();
            var bookmarks = await _userService.GetBookmarksAsync(userId, entity_type);
            return Ok(bookmarks);
        }

        /// <summary>POST /api/users/bookmarks — Thêm bookmark</summary>
        [HttpPost("bookmarks")]
        public async Task<IActionResult> CreateBookmark([FromBody] BookmarkCreateDto dto)
        {
            var userId = GetUserIdFromHeader();
            try
            {
                await _userService.CreateBookmarkAsync(userId, dto);
                return StatusCode(201);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>DELETE /api/users/bookmarks/{id} — Xóa bookmark</summary>
        [HttpDelete("bookmarks/{id:guid}")]
        public async Task<IActionResult> DeleteBookmark(Guid id)
        {
            var userId = GetUserIdFromHeader();
            await _userService.DeleteBookmarkAsync(id, userId);
            return NoContent();
        }

        // ── FOLLOWS ───────────────────────────────────────────
        /// <summary>GET /api/users/follows — Danh sách đang follow</summary>
        [HttpGet("follows")]
        public async Task<ActionResult<IEnumerable<FollowDto>>> GetFollows()
        {
            var userId = GetUserIdFromHeader();
            var follows = await _userService.GetFollowsAsync(userId);
            return Ok(follows);
        }

        /// <summary>POST /api/users/follows/keywords/{keywordId} — Follow 1 keyword</summary>
        [HttpPost("follows/keywords/{keywordId:guid}")]
        public async Task<IActionResult> FollowKeyword(Guid keywordId, [FromQuery] string? target_name)
        {
            var userId = GetUserIdFromHeader();
            await _userService.FollowKeywordAsync(userId, keywordId, target_name);
            return StatusCode(201);
        }

        /// <summary>POST /api/users/follows/journals/{journalId} — Follow 1 journal</summary>
        [HttpPost("follows/journals/{journalId:guid}")]
        public async Task<IActionResult> FollowJournal(Guid journalId, [FromQuery] string? target_name)
        {
            var userId = GetUserIdFromHeader();
            await _userService.FollowJournalAsync(userId, journalId, target_name);
            return StatusCode(201);
        }

        /// <summary>DELETE /api/users/follows/{id} — Unfollow</summary>
        [HttpDelete("follows/{id:guid}")]
        public async Task<IActionResult> Unfollow(Guid id)
        {
            var userId = GetUserIdFromHeader();
            await _userService.UnfollowAsync(id, userId);
            return NoContent();
        }

        // ── NOTIFICATIONS ─────────────────────────────────────
        /// <summary>GET /api/users/notifications — Danh sách thông báo (filter is_read)</summary>
        [HttpGet("notifications")]
        public async Task<ActionResult<IEnumerable<NotificationDto>>> GetNotifications([FromQuery] bool? is_read)
        {
            var userId = GetUserIdFromHeader();
            var notifications = await _userService.GetNotificationsAsync(userId, is_read);
            return Ok(notifications);
        }

        /// <summary>PUT /api/users/notifications/{id}/read — Đánh dấu 1 thông báo đã đọc</summary>
        [HttpPut("notifications/{id:guid}/read")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            var userId = GetUserIdFromHeader();
            await _userService.MarkAsReadAsync(id, userId);
            return NoContent();
        }

        /// <summary>PUT /api/users/notifications/read-all — Đánh dấu tất cả đã đọc</summary>
        [HttpPut("notifications/read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = GetUserIdFromHeader();
            await _userService.MarkAllAsReadAsync(userId);
            return NoContent();
        }

        /// <summary>POST /api/users/notifications/trigger — Internal only, called by PaperService</summary>
        [HttpPost("notifications/trigger")]
        public async Task<IActionResult> TriggerNotifications([FromBody] NotificationTriggerDto triggerDto)
        {
            await _userService.TriggerNotificationsAsync(triggerDto);
            return Ok(new { message = "Notifications triggered successfully" });
        }

        // ── HELPER ────────────────────────────────────────────
        private Guid GetUserIdFromHeader()
        {
            if (Request.Headers.TryGetValue("X-User-Id", out var value) && Guid.TryParse(value, out var userId))
                return userId;

            throw new UnauthorizedAccessException("Missing or invalid X-User-Id header");
        }
    }
}
