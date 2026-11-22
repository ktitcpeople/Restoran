using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RestaurantApi.Attributes;
using RestaurantApi.Database;
using RestaurantApi.Hubs;
using RestaurantApi.Models;
using RestaurantApi.Request;

namespace RestaurantApi.Controllers;

public sealed class SupportController(ContextDb db, IHubContext<KitchenHub> hub) : ControllerBase
{
    private int UserId => int.TryParse(User.Claims.FirstOrDefault(c => c.Type == "nameid")?.Value, out var id) ? id : 0;
    private int RoleId => int.TryParse(User.Claims.FirstOrDefault(c => c.Type == "role")?.Value, out var id) ? id : 0;
    private bool IsOperator => RoleId == 6;

    [HttpPost, Route("/support/chat"), RoleAuthorizeAttributes([1, 2, 3, 4, 5, 6])]
    public async Task<IActionResult> Send([FromBody] SupportChatRequest request, CancellationToken cancellationToken)
    {
        var text = request.Message?.Trim() ?? "";
        if (text.Length is < 2 or > 4000) return BadRequest(new { status = false, message = "Сообщение должно содержать от 2 до 4000 символов." });
        var conversation = request.ConversationID.HasValue
            ? await db.SupportConversations.FirstOrDefaultAsync(c => c.ConversationID == request.ConversationID && c.UserID == UserId, cancellationToken)
            : null;
        if (request.ConversationID.HasValue && conversation == null) return NotFound(new { status = false, message = "Диалог не найден." });
        conversation ??= new SupportConversation { UserID = UserId };
        if (conversation.ConversationID == 0)
        {
            db.SupportConversations.Add(conversation);
            await db.SaveChangesAsync(cancellationToken);
        }
        db.SupportMessages.Add(new SupportMessage { ConversationID = conversation.ConversationID, UserID = UserId, Sender = "user", Content = text });
        conversation.UpdatedAt = DateTime.UtcNow;

        // Каждое сообщение гостя становится видимым оператору без ручного обновления страницы.
        var ticket = await db.SupportTickets.FirstOrDefaultAsync(t => t.ConversationID == conversation.ConversationID && t.Status == "Open", cancellationToken);
        var isNewTicket = ticket == null;
        if (ticket == null)
        {
            ticket = new SupportTicket
            {
                ConversationID = conversation.ConversationID,
                UserID = UserId,
                Subject = text.Length <= 100 ? text : text[..100]
            };
            db.SupportTickets.Add(ticket);
        }
        conversation.Status = "Open";
        await db.SaveChangesAsync(cancellationToken);

        var supportEvent = new
        {
            conversationID = conversation.ConversationID,
            ticketID = ticket.TicketID,
            userID = UserId,
            subject = ticket.Subject,
            message = text,
            createdAt = conversation.UpdatedAt
        };
        await hub.Clients.Group("SupportOperators").SendAsync(
            isNewTicket ? "SupportTicketCreated" : "SupportMessageReceived",
            supportEvent,
            cancellationToken);

        return Ok(new
        {
            status = true,
            conversationID = conversation.ConversationID,
            message = "Обращение передано оператору поддержки. Ответ появится в этом чате."
        });
    }

    [HttpGet, Route("/support/conversations"), RoleAuthorizeAttributes([1, 2, 3, 4, 5, 6])]
    public async Task<IActionResult> Conversations(CancellationToken cancellationToken)
    {
        var query = db.SupportConversations.AsNoTracking();
        if (!IsOperator) query = query.Where(c => c.UserID == UserId);
        var rows = await query.OrderByDescending(c => c.UpdatedAt).Select(c => new { c.ConversationID, c.UserID, c.Status, c.CreatedAt, c.UpdatedAt }).ToListAsync(cancellationToken);
        return Ok(new { status = true, items = rows });
    }

    [HttpGet, Route("/support/conversations/{id:int}/messages"), RoleAuthorizeAttributes([1, 2, 3, 4, 5, 6])]
    public async Task<IActionResult> Messages(int id, CancellationToken cancellationToken)
    {
        var conversation = await db.SupportConversations.AsNoTracking().FirstOrDefaultAsync(c => c.ConversationID == id, cancellationToken);
        if (conversation == null) return NotFound();
        if (!IsOperator && conversation.UserID != UserId) return Forbid();
        var messages = await db.SupportMessages.AsNoTracking().Where(m => m.ConversationID == id).OrderBy(m => m.CreatedAt).ToListAsync(cancellationToken);
        return Ok(new { status = true, conversation, messages });
    }

    [HttpGet, Route("/support/tickets"), RoleAuthorizeAttributes([6])]
    public async Task<IActionResult> Tickets(CancellationToken cancellationToken)
    {
        var items = await db.SupportTickets.AsNoTracking().Where(t => t.Status == "Open")
            .OrderBy(t => t.CreatedAt).Select(t => new { t.TicketID, t.ConversationID, t.UserID, t.Subject, t.CreatedAt, t.Status }).ToListAsync(cancellationToken);
        return Ok(new { status = true, items });
    }

    [HttpPost, Route("/support/conversations/{id:int}/reply"), RoleAuthorizeAttributes([6])]
    public async Task<IActionResult> Reply(int id, [FromBody] SupportReplyRequest request, CancellationToken cancellationToken)
    {
        var text = request.Message?.Trim() ?? "";
        if (text.Length is < 1 or > 4000) return BadRequest(new { status = false, message = "Некорректный ответ." });
        var conversation = await db.SupportConversations.FirstOrDefaultAsync(c => c.ConversationID == id, cancellationToken);
        if (conversation == null) return NotFound();
        db.SupportMessages.Add(new SupportMessage { ConversationID = id, UserID = UserId, Sender = "operator", Content = text });
        conversation.UpdatedAt = DateTime.UtcNow;
        var ticket = await db.SupportTickets.FirstOrDefaultAsync(t => t.ConversationID == id && t.Status == "Open", cancellationToken);
        if (ticket != null)
        {
            ticket.OperatorID = UserId;
            if (request.Resolve) { ticket.Status = "Resolved"; ticket.ResolvedAt = DateTime.UtcNow; conversation.Status = "Resolved"; }
        }
        await db.SaveChangesAsync(cancellationToken);
        await hub.Clients.Group($"SupportUser_{conversation.UserID}").SendAsync("SupportReply", new { conversationID = id, message = text }, cancellationToken);
        return Ok(new { status = true, message = "Ответ отправлен." });
    }

}
