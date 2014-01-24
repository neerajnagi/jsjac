using System;
using ChatJs.Net;

namespace ChatJsMvcSample.Code.LongPolling.Chat
{
    /// <summary>
    /// Allows for single time chatroom event subscription
    /// </summary>
    public class ChatRoomUserTypedSubscription : IDisposable
    {
        public ChatRoom Room { get; private set; }
        public Action<string, ChatUser, ChatUser> Action { get; private set; }

        public ChatRoomUserTypedSubscription(ChatRoom room, Action<string, ChatUser, ChatUser> action)
        {
            this.Room = room;
            this.Action = action;

            this.Room.UserTyped += this.Action;
        }

        public void Dispose()
        {
            this.Room.UserTyped -= this.Action;
        }
    }
}