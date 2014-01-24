using System;
using ChatJs.Net;

namespace ChatJsMvcSample.Code.LongPolling.Chat
{
    /// <summary>
    /// Allows for single time chatroom event subscription
    /// </summary>
    public class ChatRoomMessagesSubscription : IDisposable
    {
        public ChatRoom Room { get; private set; }
        public Action<string, ChatUser, ChatUser, ChatMessage> Action { get; private set; }

        public ChatRoomMessagesSubscription(ChatRoom room, Action<string, ChatUser, ChatUser, ChatMessage> action)
        {
            this.Room = room;
            this.Action = action;

            this.Room.MessagesChanged += this.Action;
        }

        public void Dispose()
        {
            this.Room.MessagesChanged -= this.Action;
        }
    }
}