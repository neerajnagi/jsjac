using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Web.Mvc;
using ChatJs.Net;

namespace ChatJsMvcSample.Code.LongPolling.Chat
{
    public class ChatLongPollingProvider : LongPollingProvider
    {
        public override void Initialize()
        {

        }

        /// <summary>
        /// This STUB. In a normal situation, there would be multiple rooms and the user room would have to be 
        /// determined by the user profile
        /// </summary>
        public const string ROOM_ID_STUB = "chatjs-room";

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
        /// Returns a LongPollingEvent relative to the new messages.
        /// This LongPollingEvent will be sent up to the client
        /// </summary>
        /// <param name="messages"></param>
        /// <returns></returns>
        private static LongPollingEvent GetLongPollingEventForMessages(IEnumerable<ChatMessage> messages)
        {
            if (messages == null) throw new ArgumentNullException("messages");
            return new LongPollingEvent()
                {
                    Data = messages,
                    EventKey = "new-messages",
                    ProviderName = "chat"
                };
        }

        /// <summary>
        /// Returns a LongPollingEvent relative to the given users-list
        /// This LongPollingEvent will be sent up to the client
        /// </summary>
        /// <param name="users"></param>
        /// <returns></returns>
        private static LongPollingEvent GetLongPollingEventForRoomUsersChanged(IEnumerable<ChatUser> users, int myUserId)
        {
            if (users == null) throw new ArgumentNullException("users");
            var roomUsersExcludingCurrentUser = users.Where(u => u.Id != myUserId).OrderBy(u => u.Name).ToList();
            return new LongPollingEvent()
                {
                    Data = roomUsersExcludingCurrentUser,
                    EventKey = "user-list",
                    ProviderName = "chat"
                };
        }

        /// <summary>
        /// Returns a LongPollingEvent relative to a user typing
        /// This LongPollingEvent will be sent up to the client
        /// </summary>
        /// <param name="users"></param>
        /// <returns></returns>
        private static LongPollingEvent GetLongPollingEventForUserTyped(int otherUserId)
        {
            return new LongPollingEvent()
            {
                Data = otherUserId,
                EventKey = "user-typed",
                ProviderName = "chat"
            };
        }

        public override IEnumerable<LongPollingEvent> WaitForEvents(int myUserId, string roomId, long timestamp, string connectionString, Controller controller)
        {
            Debug.WriteLine("Waiting for events");

            // tells whether the message should be sent to the current user.
            // messages that the current user wrote are also ellegible
            Func<ChatMessage, bool> shouldMessageBeSentToCurrentUser = message => message.UserFromId == myUserId || message.UserToId == myUserId;

            var events = new List<LongPollingEvent>();

            // First, see if there are existing messages, in this case we're gonna
            // just return them
            var existingMessages = ChatServer.Rooms[roomId].GetMessagesAfter(timestamp).Where(shouldMessageBeSentToCurrentUser).ToList();

            // the reason why this "timestamp > 0" is that timestamp is 0 when it's the first time user
            // user is requesting messages. But I don't want to send old messages to the user as if they were
            // new
            if (existingMessages.Any() && timestamp > 0)
                events.Add(GetLongPollingEventForMessages(existingMessages));

            // now, see if the users list changed since last time
            if (ChatServer.Rooms[roomId].HasChangedSince(timestamp))
                events.Add(GetLongPollingEventForRoomUsersChanged(ChatServer.Rooms[roomId].UpdateStatusesAndGetUserList(), myUserId));

            if (!events.Any())
            {
                // In this case, nothing actually changed since last time, so we're gonna have
                // to wait
                var wait = new AutoResetEvent(false);

                // when the users change
                using (ChatServer.Rooms[roomId].SubscribeForUsersChange(
                        (r, users) =>
                        {
                            events.Add(GetLongPollingEventForRoomUsersChanged(users, myUserId));
                            // when the users list changes, it will release the thread.
                            wait.Set();
                        }))

                // when there's new message
                using (ChatServer.Rooms[roomId].SubscribeForMessagesChange(
                        (r, userFrom, userTo, message) =>
                        {
                            // all messages destinated to the given user must be considered
                            if (shouldMessageBeSentToCurrentUser(message))
                            {
                                events.Add(GetLongPollingEventForMessages(new List<ChatMessage>() {message}));
                                // when someone sends a message, will release the thread.
                                wait.Set();
                            }
                        }))

                // when someone typed something
                using (ChatServer.Rooms[roomId].SubscribeForUserTyped(
                    (s, userFrom, userTo) =>
                    {
                        // if someone is typing to the current user...
                        if (userTo.Id == myUserId)
                        {
                            events.Add(GetLongPollingEventForUserTyped(userFrom.Id));
                            wait.Set();
                        }
                    }))

                {
                    // will STOP the thread here. Only wait.Set() will release it
                    wait.WaitOne(LongPollingProvider.WAIT_TIMEOUT);
                }
            }
            return events;
        }
    }
}