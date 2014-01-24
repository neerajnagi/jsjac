using System;
using System.Collections.Generic;
using System.Diagnostics;
using ChatJs.Net;

namespace ChatJsMvcSample.Code.LongPolling.Chat
{
    /// <summary>
    /// Allows for single time chatroom event subscription 
    /// </summary>
    public class ChatRoomUsersSubscription : IDisposable
    {
        public ChatRoom Room { get; private set; }
        public Action<string, List<ChatUser>> Action { get; private set; }

        public ChatRoomUsersSubscription(ChatRoom room, Action<string, List<ChatUser>> action)
        {
            this.Room = room;
            this.Action = action;
            
            this.Room.UserStatusChanged += this.Action;
        }

        public void Dispose()
        {
            this.Room.UserStatusChanged -= this.Action;
        }
    }
}