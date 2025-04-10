using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using veterans_site.Data;
using veterans_site.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

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

            if (string.IsNullOrEmpty(senderId))
                return;

            message = message ?? string.Empty;

            if (receiverId == "general-chat")
            {
                var sender = await _context.Users
                    .Where(u => u.Id == senderId)
                    .Select(u => new {
                        u.Id,
                        u.FirstName,
                        u.LastName,
                        u.AvatarPath
                    })
                    .FirstOrDefaultAsync();
                    
                if (sender == null) return;

                var userManager = Context.GetHttpContext().RequestServices
                    .GetRequiredService<UserManager<ApplicationUser>>();
                var senderUser = await userManager.FindByIdAsync(senderId);
                var senderRoles = await userManager.GetRolesAsync(senderUser);
                var senderRole = senderRoles.FirstOrDefault();
                string roleName = "Користувач";

                switch (senderRole)
                {
                    case "Admin":
                        roleName = "Адмін";
                        break;
                    case "Veteran":
                        roleName = "Ветеран";
                        break;
                    case "Specialist":
                        roleName = "Спеціаліст";
                        break;
                    case "Volunteer":
                        roleName = "Волонтер";
                        break;
                }

                var generalChatMessage = new ChatMessage
                {
                    SenderId = senderId,
                    ReceiverId = null,
                    Content = message,
                    SentAt = DateTime.Now,
                    FilePath = fileUrl,
                    FileName = fileName,
                    FileType = fileType,
                    FileSize = fileSize,
                    IsGeneralChat = true 
                };
                
                _context.ChatMessages.Add(generalChatMessage);
                
                try {
                    await _context.SaveChangesAsync();
            
                    var messageData = new
                    {
                        MessageId = generalChatMessage.Id,
                        SenderId = sender.Id,
                        SenderName = $"{sender.FirstName} {sender.LastName}",
                        SenderAvatar = sender.AvatarPath,
                        Content = message,
                        SentAt = generalChatMessage.SentAt,
                        FileUrl = fileUrl,
                        FileName = fileName,
                        FileType = fileType,
                        FileSize = fileSize,
                        SenderInfo = new
                        {
                            Role = roleName
                        }
                    };
            
                    await Clients.Caller.SendAsync("ReceiveMessage", messageData);
                    await Clients.AllExcept(Context.ConnectionId).SendAsync("ReceiveMessage", 
                        new {
                            MessageId = generalChatMessage.Id,
                            SenderId = sender.Id,
                            SenderName = $"{sender.FirstName} {sender.LastName}",
                            SenderAvatar = sender.AvatarPath,
                            Content = message,
                            SentAt = generalChatMessage.SentAt,
                            FileUrl = fileUrl,
                            FileName = fileName,
                            FileType = fileType,
                            FileSize = fileSize,
                            SenderInfo = new {
                                Role = roleName
                            }
                        }
                    );
            
                    var notificationData = new {
                        SenderId = sender.Id,
                        SenderName = $"{sender.FirstName} {sender.LastName}",
                        SenderAvatar = sender.AvatarPath,
                        MessagePreview = message.Length > 50 ? message.Substring(0, 50) + "..." : message,
                        SentAt = generalChatMessage.SentAt.ToString("HH:mm"),
                        IsGeneralChat = true
                    };
            
                    await Clients.AllExcept(Context.ConnectionId).SendAsync("NewGeneralChatNotification", notificationData);
            
                    await Clients.AllExcept(Context.ConnectionId).SendAsync("UpdateUnreadMessageCount", "general-chat");
            
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending general chat message: {ex.Message}");
                    throw;
                }
            }

            var receiverExists = await _context.Users.AnyAsync(u => u.Id == receiverId);
            if (!receiverExists)
            {
                await Clients.Caller.SendAsync("ErrorMessage", "Recipient not found.");
                return;
            }

            var chatMessage = new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = message,
                SentAt = DateTime.Now,
                FilePath = fileUrl,
                FileName = fileName,
                FileType = fileType,
                FileSize = fileSize,
                IsGeneralChat = false
            };
            
            _context.ChatMessages.Add(chatMessage);
            
            try
            {
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
                
                var receiverConnections = _connections.Values
                    .Where(c => c.UserId == receiverId)
                    .Select(c => c.ConnectionId)
                    .ToList();
                
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
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending private message: {ex.Message}");
                throw;
            }
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

            if (message.IsGeneralChat)
            {
                await Clients.All.SendAsync("MessageEdited", messageId, newContent);
            }
            else
            {
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

            await _context.SaveChangesAsync();

            if (message.IsGeneralChat)
            {
                await Clients.All.SendAsync("MessageDeleted", messageId);
            }
            else
            {
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
        
        public async Task MarkGeneralChatMessagesAsRead()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return;

            var lastMessageId = await _context.ChatMessages
                .Where(m => m.IsGeneralChat)
                .OrderByDescending(m => m.Id)
                .Select(m => m.Id)
                .FirstOrDefaultAsync();

            var lastRead = await _context.UserLastReadGeneralChats
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (lastRead == null)
            {
                lastRead = new UserLastReadGeneralChat
                {
                    UserId = userId,
                    LastReadMessageId = lastMessageId,
                    LastReadAt = DateTime.Now
                };
                _context.UserLastReadGeneralChats.Add(lastRead);
            }
            else
            {
                lastRead.LastReadMessageId = lastMessageId;
                lastRead.LastReadAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            await Clients.Caller.SendAsync("GeneralChatMessageRead");
        }
        
        public async Task RegisterConnection()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                _connections[Context.ConnectionId] = new UserConnection
                {
                    ConnectionId = Context.ConnectionId,
                    UserId = userId
                };
            }
        }
        
        public async Task SendGeneralChatNotification(string senderId, string senderName, string senderAvatar, string messagePreview, string sentAt)
        {
            var currentUserId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    
            if (currentUserId == senderId)
            {
                return;
            }
    
            var notificationData = new {
                SenderId = senderId,
                SenderName = senderName,
                SenderAvatar = senderAvatar,
                MessagePreview = messagePreview,
                SentAt = sentAt,
                IsGeneralChat = true
            };
    
            await Clients.Others.SendAsync("NewGeneralChatNotification", notificationData);
        }
    }
}
