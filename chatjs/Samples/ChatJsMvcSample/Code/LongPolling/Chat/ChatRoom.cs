using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ChatJs.Net;

namespace ChatJsMvcSample.Code.LongPolling.Chat
{
    /// <summary>
    /// A Chat room. Chat room users are STATIC, meaning they don't change, unless the practice adds another user. A user doesn't join
    /// a room. It's in a room automatically. The only thing that changes is the status.
    /// </summary>
    public class ChatRoom
    {
        /// <summary>
        /// Time after which the user is considered inactive and elegible for 
        /// removal (in seconds)
        /// </summary>
        private static readonly TimeSpan InactivityTolerance = TimeSpan.FromSeconds(40);

        readonly object usersInRoomLock = new object();
        readonly object messageLock = new object();
        private long lastTimeRoomChanged = DateTime.UtcNow.Ticks;

        public ChatRoom(string id)
        {
            this.Id = id;
            this.UsersById = new Dictionary<int, ChatUser>();
            this.Messages = new List<ChatMessage>();
        }

        /// <summary>
        /// Total list of users. Include offline
        /// </summary>
        public Dictionary<int, ChatUser> UsersById { get; private set; }

        /// <summary>
        /// All the messages in the room, from all users.
        /// </summary>
        /// <remarks>
        /// This data structure is definitely not the better one for this purpose.
        /// ToDo: fix this
        /// </remarks>
        public List<ChatMessage> Messages { get; private set; }

        /// <summary>
        /// Chat room Id. 
        /// This Id corresponds to the Practice Id
        /// </summary>
        private string Id { get; set; }

        /// <summary>
        /// Adds a user to the room
        /// </summary>
        public void RegisterNewUser(ChatUser user)
        {
            lock (this.usersInRoomLock)
            {
                if (user == null) throw new ArgumentNullException("user");

                if (this.UsersById.ContainsKey(user.Id))
                    throw new Exception("User already existis in the room. User id:" + user.Id);

                this.UsersById.Add(user.Id, user);
                user.LastActiveOn = DateTime.UtcNow;
                // I'm infering he/she is online now by the usage of this method. I'm not sure this will work
                this.NotifyUsersChanged();
            }
        }

        /// <summary>
        /// Removes a user from the room
        /// </summary>
        public void RemoveUser(ChatUser user)
        {
            lock (this.usersInRoomLock)
            {
                if (user == null) throw new ArgumentNullException("user");

                this.UsersById.Remove(user.Id);
                this.NotifyUsersChanged();
            }
        }

        /// <summary>
        /// Returns whether or not the current user exists
        /// </summary>
        /// <param name="userId"></param>
        public bool UserExists(int userId)
        {
            lock (this.usersInRoomLock)
            {
                return this.UsersById.ContainsKey(userId);
            }
        }

        /// <summary>
        /// Adds a user to the room. If the user is in the room already, updates his/her
        /// LastActiveOn
        /// </summary>
        public void SetUserOnline(int userId)
        {
            lock (this.usersInRoomLock)
            {
                if (!this.UsersById.ContainsKey(userId))
                    throw new Exception("User not found in the room. User id:" + userId);

                this.UsersById[userId].LastActiveOn = DateTime.UtcNow;

                // if this user wasn't online previously, make it online and tell everyone
                if (this.UsersById[userId].Status != ChatUser.StatusType.Online)
                {
                    this.UsersById[userId].Status = ChatUser.StatusType.Online;
                    this.NotifyUsersChanged();
                }
            }
        }

        /// <summary>
        /// Removes the given user
        /// </summary>
        public void SetUserOffline(int userId)
        {
            lock (this.usersInRoomLock)
            {
                if (!this.UsersById.ContainsKey(userId))
                    throw new Exception("User not found in the room. User id:" + userId);

                var user = this.UsersById[userId];

                if (user.Status == ChatUser.StatusType.Offline)
                    return;
                user.Status = ChatUser.StatusType.Offline;
                this.NotifyUsersChanged();
            }
        }

        /// <summary>
        /// Returns all users in the room sorted by name.
        /// This will also update the status of the unseen users to offline.
        /// </summary>
        public List<ChatUser> UpdateStatusesAndGetUserList()
        {
            lock (this.usersInRoomLock)
            {
                var lastSeenLimit = DateTime.UtcNow - InactivityTolerance;
                var inactiveUsers = this.UsersById.Values.Where(u => u.LastActiveOn < lastSeenLimit).ToArray();

                // inactive users will be removed, but they could just be made offline
                foreach (var inactiveUser in inactiveUsers)
                    UsersById.Remove(inactiveUser.Id);

                if (inactiveUsers.Any())
                    this.lastTimeRoomChanged = DateTime.UtcNow.Ticks;

                return this.UsersById.Values.OrderBy(u => u.Name).ToList();
            }
        }

        /// <summary>
        /// Returns the messages related to the given user.
        /// </summary>
        /// <param name="myUserId"></param>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        public IEnumerable<ChatMessage> GetMessagesAfter(long timestamp)
        {
            lock (this.messageLock)
            {
                var nowTimeStamp = DateTime.UtcNow.Ticks;
                return this.Messages.Where(m => m.Timestamp > timestamp && m.Timestamp <= nowTimeStamp);
            }
        }

        /// <summary>
        /// Returns all the messages between the two users given, that occurred PRIOR to the passed timeStamp.
        /// This list is INVERTED to make things easier in the client
        /// </summary>
        /// <param name="myUserId"></param>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        public List<ChatMessage> GetMessagesBetween(int myUserId, int otherUserId, long? timestamp = null)
        {
            lock (this.messageLock)
            {
                var query = this.Messages.Where(m => (m.UserToId == myUserId && m.UserFromId == otherUserId) || (m.UserToId == otherUserId && m.UserFromId == myUserId));
                if (timestamp.HasValue)
                    query = query.Where(m => m.Timestamp < timestamp);
                return query.OrderBy(m => m.Timestamp).ToList();
            }
        }

        /// <summary>
        /// Sends a message to a user
        /// </summary>
        /// <param name="userFromId"></param>
        /// <param name="userToId"></param>
        /// <param name="message"></param>
        /// <param name="clientGuid"></param>
        public void SendMessage(int userFromId, int userToId, string message, string clientGuid)
        {
            if (message == null) throw new ArgumentNullException("message");
            if (clientGuid == null) throw new ArgumentNullException("clientGuid");

            lock (this.messageLock)
            {
                var newMessage = new ChatMessage()
                {
                    Message = message,
                    Timestamp = DateTime.UtcNow.Ticks,
                    UserFromId = userFromId,
                    UserToId = userToId,
                    ClientGuid = clientGuid
                };

                this.Messages.Add(newMessage);

                var userFrom = this.UsersById[userFromId];
                var userTo = this.UsersById[userToId];

                if (this.MessagesChanged != null)
                    this.MessagesChanged(this.Id, userFrom, userTo, newMessage);
            }
        }

        /// <summary>
        /// Sends a typing signal to a user
        /// </summary>
        /// <param name="userFromId"></param>
        /// <param name="userToId"></param>
        public void SendTypingSignal(int userFromId, int userToId)
        {
            var userFrom = this.UsersById[userFromId];
            var userTo = this.UsersById[userToId];

            this.UserTyped(this.Id, userFrom, userTo);
        }

        /// <summary>
        /// Event that is triggered when a room changed
        /// </summary>
        public event Action<string, List<ChatUser>> UserStatusChanged;

        /// <summary>
        /// This method is supposed to be used inside a 'using' clause.
        /// </summary>
        /// <param name="onUserStatusChanged"></param>
        /// <returns></returns>
        public ChatRoomUsersSubscription SubscribeForUsersChange(Action<string, List<ChatUser>> onUserStatusChanged)
        {
            if (onUserStatusChanged == null) throw new ArgumentNullException("onUserStatusChanged");
            return new ChatRoomUsersSubscription(this, onUserStatusChanged);
        }

        /// <summary>
        /// Event that is triggered when a user types
        /// </summary>
        public event Action<string, ChatUser, ChatUser> UserTyped;

        /// <summary>
        /// This method is supposed to be used inside a 'using' clause.
        /// </summary>
        /// <returns></returns>
        public ChatRoomUserTypedSubscription SubscribeForUserTyped(Action<string, ChatUser, ChatUser> onUserTyped)
        {
            if (onUserTyped == null) throw new ArgumentNullException("onUserTyped");
            return new ChatRoomUserTypedSubscription(this, onUserTyped);
        }

        /// <summary>
        /// Notifies subscribers
        /// </summary>
        private void NotifyUsersChanged()
        {
            this.lastTimeRoomChanged = DateTime.UtcNow.Ticks;
            if (this.UserStatusChanged != null)
            {
                Debug.WriteLine("There was someone listening to the event");
                var usersList = this.UpdateStatusesAndGetUserList();
                this.UserStatusChanged(this.Id, usersList);
            }
            else
            {
                Debug.WriteLine("There was noone listening for the event");
            }
        }

        /// <summary>
        /// Event that is triggered when a room changed
        /// </summary>
        public event Action<string, ChatUser, ChatUser, ChatMessage> MessagesChanged;

        /// <summary>
        /// This method is supposed to be used inside a 'using' clause.
        /// </summary>
        /// <param name="onNewMessage"></param>
        /// <returns></returns>
        public ChatRoomMessagesSubscription SubscribeForMessagesChange(Action<string, ChatUser, ChatUser, ChatMessage> onNewMessage)
        {
            if (onNewMessage == null) throw new ArgumentNullException("onNewMessage");
            return new ChatRoomMessagesSubscription(this, onNewMessage);
        }
        
        /// <summary>
        /// Indicates whether or not the users in this room changed since the given timestamp
        /// </summary>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        public bool HasChangedSince(long timestamp)
        {
            return this.lastTimeRoomChanged > timestamp;
        }
    }
}