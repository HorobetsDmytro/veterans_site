using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
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
                
                _connections.AddOrUpdate(userId, connection, (key, oldValue) => connection);
                
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.IsOnline = true;
                    user.LastOnline = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
                
                await Clients.All.SendAsync("UserStatusChanged", userId, true, DateTime.Now);
            }
            
            await base.OnConnectedAsync();
        }
        
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (!string.IsNullOrEmpty(userId) && _connections.TryRemove(userId, out var connection))
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.IsOnline = false;
                    user.LastOnline = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
                
                await Clients.All.SendAsync("UserStatusChanged", userId, false, DateTime.Now);
            }
            
            await base.OnDisconnectedAsync(exception);
        }
        
        public async Task SendPrivateMessage(string receiverId, string message, string fileUrl = null, string fileName = null, string fileType = null)
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
            };
            
            chatMessage.FilePath = fileUrl;
            chatMessage.FileName = fileName;
            chatMessage.FileType = fileType;
    
            _context.ChatMessages.Add(chatMessage);
            try 
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving message: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            
                throw;
            }
            
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
            
            if (_connections.TryGetValue(receiverId, out var receiverConnection))
            {
                await Clients.Client(receiverConnection.ConnectionId).SendAsync("ReceiveMessage", 
                    chatMessage.Id,
                    sender.Id,
                    $"{sender.FirstName} {sender.LastName}",
                    sender.AvatarPath,
                    message,
                    chatMessage.SentAt,
                    fileUrl,
                    fileName,
                    fileType
                    );
            }
            
            await Clients.Caller.SendAsync("MessageSent", 
                chatMessage.Id,
                receiverId,
                message,
                chatMessage.SentAt,
                fileUrl,
                fileName,
                fileType
                );
        }
        
        public async Task MarkMessageAsRead(int messageId)
        {
            var message = await _context.ChatMessages.FindAsync(messageId);
            if (message != null)
            {
                message.IsRead = true;
                await _context.SaveChangesAsync();
                
                if (_connections.TryGetValue(message.SenderId, out var senderConnection))
                {
                    await Clients.Client(senderConnection.ConnectionId).SendAsync("MessageRead", messageId);
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
            
            if (_connections.TryGetValue(message.ReceiverId, out var receiverConnection))
            {
                await Clients.Client(receiverConnection.ConnectionId).SendAsync("MessageEdited", messageId, newContent);
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
            
            if (_connections.TryGetValue(message.ReceiverId, out var receiverConnection))
            {
                await Clients.Client(receiverConnection.ConnectionId).SendAsync("MessageDeleted", messageId);
            }
            
            await Clients.Caller.SendAsync("MessageDeleted", messageId);
        }
    }
}