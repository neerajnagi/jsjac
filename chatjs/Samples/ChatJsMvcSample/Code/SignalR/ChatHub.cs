using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChatJs.Net;
using Microsoft.AspNet.SignalR;

namespace ChatJsMvcSample.Code.SignalR
{
    public class ChatHub : Hub, IChatHub
    {
        /// <summary>
        /// This STUB. In a normal situation, there would be multiple rooms and the user room would have to be 
        /// determined by the user profile
        /// </summary>
        public const string ROOM_ID_STUB = "chatjs-room";

        /// <summary>
        /// Current connections
        /// 1 room has many users that have many connections (2 open browsers from the same user represents 2 connections)
        /// </summary>
        private static readonly Dictionary<string, Dictionary<int, List<string>>> connections = new Dictionary<string, Dictionary<int, List<string>>>();

        /// <summary>
        /// This is STUB. This will SIMULATE a database of chat messages
        /// </summary>
        private static readonly List<ChatMessage> chatMessages = new List<ChatMessage>();

        /// <summary>
        /// This method is STUB. This will SIMULATE a database of users
        /// </summary>
        private static readonly List<ChatUser> chatUsers = new List<ChatUser>();

        /// <summary>
        /// This method is STUB. In a normal situation, the user info would come from the database so this method wouldn't be necessary.
        /// It's only necessary because this class is simulating the database
        /// </summary>
        /// <param name="newUser"></param>
        public static void RegisterNewUser(ChatUser newUser)
        {
            if (newUser == null) throw new ArgumentNullException("newUser");
            chatUsers.Add(newUser);
        }

        /// <summary>
        /// This method is STUB. Returns if a user is registered in the FAKE DB.
        /// Normally this wouldn't be necessary.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public static bool IsUserRegistered(ChatUser user)
        {
            return chatUsers.Any(u => u.Id == user.Id);
        }

        /// <summary>
        /// Tries to find a user with the provided e-mail
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public static ChatUser FindUserByEmail(string email)
        {
            return string.IsNullOrEmpty(email) ? null : chatUsers.FirstOrDefault(u => u.Email == email);
        }

        /// <summary>
        /// If the specified user is connected, return information about the user
        /// </summary>
        public ChatUser GetUserInfo(int userId)
        {
            var user = chatUsers.FirstOrDefault(u => u.Id == userId);
            return user == null ? null : this.GetUserById(userId);
        }

        private ChatUser GetUserById(int id)
        {
            var myRoomId = this.GetMyRoomId();

            // this is STUB. Normally you would go to the database get the real user
            var dbUser = chatUsers.First(u => u.Id == id);

            ChatUser.StatusType userStatus;
            lock (connections)
            {
                userStatus = connections.ContainsKey(myRoomId)
                                 ? (connections[myRoomId].ContainsKey(dbUser.Id)
                                        ? ChatUser.StatusType.Online
                                        : ChatUser.StatusType.Offline)
                                 : ChatUser.StatusType.Offline;
            }
            return new ChatUser()
            {
                Id = dbUser.Id,
                Name = dbUser.Name,
                Status = userStatus,
                ProfilePictureUrl = GravatarHelper.GetGravatarUrl(GravatarHelper.GetGravatarHash(dbUser.Email), GravatarHelper.Size.s32)
            };
        }

        /// <summary>
        /// Returns my user id
        /// </summary>
        /// <returns></returns>
        private int GetMyUserId()
        {
            // This would normally be done like this:
            //var userPrincipal = this.Context.User as AuthenticatedPrincipal;
            //if (userPrincipal == null)
            //    throw new NotAuthorizedException();

            //var userData = userPrincipal.Profile;
            //return userData.Id;

            // But for this example, it will get my user from the cookie
            return ChatHelper.GetChatUserFromCookie(this.Context.Request).Id;
        }

        private string GetMyRoomId()
        {
            // This would normally be done like this:
            //var userPrincipal = this.Context.User as AuthenticatedPrincipal;
            //if (userPrincipal == null)
            //    throw new NotAuthorizedException();

            //var userData = userPrincipal.Profile;
            //return userData.MyTenancyIdentifier;

            // But for this example, it will always return "chatjs-room", because we have only one room.
            return ROOM_ID_STUB;
        }

        /// <summary>
        /// Broadcasts to all users in the same room the new users list
        /// </summary>
        /// <param name="myUserId">
        /// user Id that has to be excluded in the broadcast. That is, all users
        /// should receive the message, except this.
        /// </param>
        private void NotifyUsersChanged()
        {
            var myRoomId = this.GetMyRoomId();
            var myUserId = this.GetMyUserId();


            if (connections.ContainsKey(myRoomId))
            {
                foreach (var userId in connections[myRoomId].Keys)
                {
                    // we don't want to broadcast to the current user
                    if (userId == myUserId)
                        continue;

                    var userIdClusure = userId;

                    // creates a list of users that contains all users with the exception of the user to which 
                    // the list will be sent
                    // every user will receive a list of user that exclude him/hearself
                    var usersList = chatUsers.Where(u => u.Id != userIdClusure);

                    if (connections[myRoomId][userId] != null)
                        foreach (var connectionId in connections[myRoomId][userId])
                            this.Clients.Client(connectionId).usersListChanged(usersList);
                }
            }

        }

        private ChatMessage PersistMessage(int otherUserId, string message, string clientGuid)
        {
            var myUserId = this.GetMyUserId();

            // this is STUB. Normally you would go to the real database to get the my user and the other user
            var myUser = chatUsers.FirstOrDefault(u => u.Id == myUserId);
            var otherUser = chatUsers.FirstOrDefault(u => u.Id == otherUserId);

            if (myUser == null || otherUser == null)
                return null;

            var chatMessage = new ChatMessage
            {
                DateTime = DateTime.UtcNow,
                Message = message,
                ClientGuid = clientGuid,
                UserFromId = myUserId,
                UserToId = otherUserId,
            };

            // this is STUB. Normally you would add the dbMessage to the real database
            chatMessages.Add(chatMessage);

            // normally you would save the database changes
            //this.db.SaveChanges();

            return chatMessage;
        }

        #region IChatHub

        /// <summary>
        /// Returns the message history
        /// </summary>
        public List<ChatMessage> GetMessageHistory(int otherUserId)
        {
            var myUserId = this.GetMyUserId();
            // this is STUB. Normally you would go to the real database to get the messages
            var messages = chatMessages
                               .Where(
                                   m =>
                                   (m.UserToId == myUserId && m.UserFromId == otherUserId) ||
                                   (m.UserToId == otherUserId && m.UserFromId == myUserId))
                               .OrderByDescending(m => m.Timestamp).Take(30).ToList();

            messages.Reverse();
            return messages;
        }

        /// <summary>
        /// Returns the message history
        /// </summary>
        public List<ChatUser> GetUsersList()
        {
            var myUserId = this.GetMyUserId();
            var myRoomId = this.GetMyRoomId();
            var roomUsers = chatUsers.Where(u => u.RoomId == myRoomId && u.Id != myUserId).OrderBy(u => u.Name).ToList();
            return roomUsers;
        }

        /// <summary>
        /// Sends a message to a particular user
        /// </summary>
        public void SendMessage(int otherUserId, string message, string clientGuid)
        {
            var myUserId = this.GetMyUserId();
            var myRoomId = this.GetMyRoomId();

            var dbChatMessage = this.PersistMessage(otherUserId, message, clientGuid);
            var connectionIds = new List<string>();
            lock (connections)
            {
                if (connections[myRoomId].ContainsKey(otherUserId))
                    connectionIds.AddRange(connections[myRoomId][otherUserId]);
                if (connections[myRoomId].ContainsKey(myUserId))
                    connectionIds.AddRange(connections[myRoomId][myUserId]);
            }
            foreach (var connectionId in connectionIds)
                this.Clients.Client(connectionId).sendMessage(dbChatMessage);
        }

        /// <summary>
        /// Sends a typing signal to a particular user
        /// </summary>
        public void SendTypingSignal(int otherUserId)
        {
            var myUserId = this.GetMyUserId();
            var myRoomId = this.GetMyRoomId();

            var connectionIds = new List<string>();
            lock (connections)
            {
                if (connections[myRoomId].ContainsKey(otherUserId))
                    connectionIds.AddRange(connections[myRoomId][otherUserId]);
            }
            foreach (var connectionId in connectionIds)
                this.Clients.Client(connectionId).sendTypingSignal(myUserId);
        }

        /// <summary>
        /// Triggered when the user opens a new browser window
        /// </summary>
        /// <returns></returns>
        public override Task OnConnected()
        {
            var myRoomId = this.GetMyRoomId();
            var myUserId = this.GetMyUserId();

            lock (connections)
            {
                if (!connections.ContainsKey(myRoomId))
                    connections[myRoomId] = new Dictionary<int, List<string>>();

                if (!connections[myRoomId].ContainsKey(myUserId))
                {
                    // in this case, this is a NEW connection for the current user,
                    // not another browser window opening
                    connections[myRoomId][myUserId] = new List<string>();
                    this.NotifyUsersChanged();
                }
                connections[myRoomId][myUserId].Add(this.Context.ConnectionId);
            }

            return base.OnConnected();
        }

        /// <summary>
        /// Triggered when the user closes the browser window
        /// </summary>
        /// <returns></returns>
        public override Task OnDisconnected()
        {
            var myRoomId = this.GetMyRoomId();
            var myUserId = this.GetMyUserId();

            lock (connections)
            {
                if (connections.ContainsKey(myRoomId))
                    if (connections[myRoomId].ContainsKey(myUserId))
                        if (connections[myRoomId][myUserId].Contains(this.Context.ConnectionId))
                        {
                            connections[myRoomId][myUserId].Remove(this.Context.ConnectionId);
                            if (!connections[myRoomId][myUserId].Any())
                            {
                                connections[myRoomId].Remove(myUserId);
                                Task.Run(() =>
                                    {
                                        // this will run in separate thread.
                                        // If the user is away for more than 10 seconds it will be removed from 
                                        // the room.
                                        // In a normal situation this wouldn't be done because normally the users in a
                                        // chat room are fixed, like when you have 1 chat room for each tenancy
                                        Thread.Sleep(10000);
                                        if (!connections[myRoomId].ContainsKey(myUserId))
                                        {
                                            var myDbUser = chatUsers.FirstOrDefault(u => u.Id == myUserId);
                                            if (myDbUser != null)
                                            {
                                                chatUsers.Remove(myDbUser);
                                                this.NotifyUsersChanged();
                                            }
                                        }
                                    });
                            }
                        }
            }

            return base.OnDisconnected();
        }

        #endregion
    }
}