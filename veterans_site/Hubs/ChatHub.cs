using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using veterans_site.Data;
using veterans_site.Models;
using System.Security.Claims;

namespace veterans_site.Hubs
{
    public class ChatHub : Hub
    {
        private static readonly ConcurrentDictionary<string, UserConnection> _connections = 
            new ConcurrentDictionary<string, UserConnection>();
            
        private readonly VeteranSupportDbContext _context;
        
        public ChatHub(VeteranSupportDbContext context)
        {
            _context = context;
        }
        
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                var connection = new UserConnection
                {
                    UserId = userId,
                    ConnectionId = Context.ConnectionId,
                    IsOnline = true,
                    LastSeen = DateTime.Now
                };

                var existingConnection = _connections.Values
                    .FirstOrDefault(c => c.UserId == userId && c.ConnectionId != Context.ConnectionId);

                _connections.AddOrUpdate(Context.ConnectionId, connection, (key, oldValue) => connection);

                if (existingConnection == null)
                {
                    var user = await _context.Users.FindAsync(userId);
                    if (user != null)
                    {
                        user.IsOnline = true;
                        user.LastOnline = DateTime.Now;
                        await _context.SaveChangesAsync();
                
                        await Clients.All.SendAsync("UserStatusChanged", userId, true, DateTime.Now);
                    }
                }
            }

            await base.OnConnectedAsync();
        }
        
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    
            if (!string.IsNullOrEmpty(userId))
            {
                _connections.TryRemove(Context.ConnectionId, out _);
        
                var hasOtherConnections = _connections.Values
                    .Any(c => c.UserId == userId);
        
                if (!hasOtherConnections)
                {
                    var user = await _context.Users.FindAsync(userId);
                    if (user != null)
                    {
                        user.IsOnline = false;
                        user.LastOnline = DateTime.Now;
                        await _context.SaveChangesAsync();
                
                        await Clients.All.SendAsync("UserStatusChanged", userId, false, DateTime.Now);
                    }
                }
            }
    
            await base.OnDisconnectedAsync(exception);
        }
        
        public async Task SendPrivateMessage(string receiverId, string message, string fileUrl = null, 
            string fileName = null, string fileType = null, string fileSize = null)
        {
            var senderId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(senderId) || string.IsNullOrEmpty(receiverId))
                return;

            message = message ?? string.Empty;

            var chatMessage = new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = message ?? string.Empty,
                SentAt = DateTime.Now,
                FilePath = fileUrl,
                FileName = fileName,
                FileType = fileType,
                FileSize = fileSize
            };
            
            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();
            
            var user = await _context.Users.FindAsync(senderId);
            if (user != null)
            {
                user.LastOnline = DateTime.Now;
                await _context.SaveChangesAsync();
            }
            
            var sender = await _context.Users
                .Where(u => u.Id == senderId)
                .Select(u => new {
                    u.Id,
                    u.FirstName,
                    u.LastName,
                    u.AvatarPath
                })
                .FirstOrDefaultAsync();
            
            var receiverConnections = _connections.Values.Where(c => c.UserId == receiverId).Select(c => c.ConnectionId).ToList();
            
            if (receiverConnections.Any())
            {
                foreach(var connectionId in receiverConnections)
                {
                    await Clients.Client(connectionId).SendAsync("ReceiveMessage", 
                        chatMessage.Id,
                        sender.Id,
                        $"{sender.FirstName} {sender.LastName}",
                        sender.AvatarPath,
                        message,
                        chatMessage.SentAt,
                        fileUrl,
                        fileName,
                        fileType,
                        fileSize
                    );
                }
            }
            
            await Clients.Caller.SendAsync("MessageSent", 
                chatMessage.Id,
                receiverId,
                message,
                chatMessage.SentAt,
                fileUrl,
                fileName,
                fileType,
                fileSize
            );
            
            await Clients.All.SendAsync("UpdateUnreadMessageCount", receiverId);
        }
        
        public async Task MarkMessageAsRead(int messageId)
        {
            var message = await _context.ChatMessages.FindAsync(messageId);
            if (message != null && !message.IsRead)
            {
                message.IsRead = true;
                await _context.SaveChangesAsync();

                var senderConnections = _connections.Values
                    .Where(c => c.UserId == message.SenderId)
                    .Select(c => c.ConnectionId)
                    .ToList();
            
                foreach (var connectionId in senderConnections)
                {
                    await Clients.Client(connectionId).SendAsync("MessageRead", messageId);
                }
            }
        }
        
        public async Task EditMessage(int messageId, string newContent)
        {
            var senderId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    
            if (string.IsNullOrEmpty(senderId))
                return;
        
            var message = await _context.ChatMessages
                .FirstOrDefaultAsync(m => m.Id == messageId && m.SenderId == senderId);
        
            if (message == null)
                return;
        
            message.Content = newContent;
            message.IsEdited = true;
            message.EditedAt = DateTime.Now;
    
            await _context.SaveChangesAsync();
    
            var receiverConnections = _connections.Values
                .Where(c => c.UserId == message.ReceiverId)
                .Select(c => c.ConnectionId)
                .ToList();
    
            foreach (var connectionId in receiverConnections)
            {
                await Clients.Client(connectionId).SendAsync("MessageEdited", messageId, newContent);
            }
    
            await Clients.Caller.SendAsync("MessageEdited", messageId, newContent);
        }

        public async Task DeleteMessage(int messageId)
        {
            var senderId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    
            if (string.IsNullOrEmpty(senderId))
                return;
        
            var message = await _context.ChatMessages
                .FirstOrDefaultAsync(m => m.Id == messageId && m.SenderId == senderId);
        
            if (message == null)
                return;
        
            message.IsDeleted = true;
            message.Content = "Це повідомлення було видалено";
    
            await _context.SaveChangesAsync();
    
            var receiverConnections = _connections.Values
                .Where(c => c.UserId == message.ReceiverId)
                .Select(c => c.ConnectionId)
                .ToList();
    
            foreach (var connectionId in receiverConnections)
            {
                await Clients.Client(connectionId).SendAsync("MessageDeleted", messageId);
            }
    
            await Clients.Caller.SendAsync("MessageDeleted", messageId);
        }
        
        public async Task UpdateAllUsersStatus()
        {
            var onlineUsers = _connections.Keys.ToList();
            var allUsers = await _context.Users.ToListAsync();

            var statusUpdates = allUsers.Select(user => new
            {
                UserId = user.Id,
                IsOnline = onlineUsers.Contains(user.Id),
                LastSeen = user.LastOnline
            }).ToList();

            await Clients.Caller.SendAsync("ReceiveAllUsersStatus", statusUpdates);
        }
        
        public async Task NotifyUnreadMessagesCount(string userId)
        {
            var count = await _context.ChatMessages
                .CountAsync(m => m.ReceiverId == userId && !m.IsRead);
        
            if (_connections.TryGetValue(userId, out var connection))
            {
                await Clients.Client(connection.ConnectionId).SendAsync("UpdateUnreadCount", count);
            }
        }
        
        public async Task UpdateAllUnreadCounts()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return;
        
            var unreadMessagesByUser = await _context.ChatMessages
                .Where(m => m.ReceiverId == userId && !m.IsRead)
                .GroupBy(m => m.SenderId)
                .Select(g => new {
                    SenderId = g.Key,
                    UnreadCount = g.Count()
                })
                .ToListAsync();
        
            await Clients.Caller.SendAsync("UpdateAllUnreadCounts", unreadMessagesByUser);
        }
    }
}
