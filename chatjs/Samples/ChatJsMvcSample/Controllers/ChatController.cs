using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Web;
using System.Web.Mvc;
using ChatJs.Net;
using ChatJsMvcSample.Code;
using ChatJsMvcSample.Code.LongPolling;
using ChatJsMvcSample.Code.LongPolling.Chat;

namespace ChatJsMvcSample.Controllers
{
    /// <summary>
    /// ChatController
    /// THIS CONTROLLER IS ONLY USED FOR LONG-POLLING. IF YOU ARE NOT USING LONG POLLING, THIS CONTROLLER WILL
    /// NOT BE USED
    /// </summary>
    public class ChatController : Controller
    {
        /// <summary>
        /// This STUB. In a normal situation, there would be multiple rooms and the user room would have to be 
        /// determined by the user profile
        /// </summary>
        public const string ROOM_ID_STUB = "chatjs-room";

        /// <summary>
        /// Returns my user id
        /// </summary>
        /// <returns></returns>
        private int GetMyUserId(HttpRequestBase request)
        {
            // This would normally be done like this:
            //var userPrincipal = this.Context.User as AuthenticatedPrincipal;
            //if (userPrincipal == null)
            //    throw new NotAuthorizedException();

            //var userData = userPrincipal.Profile;
            //return userData.Id;

            // But for this example, it will get my user from the cookie
            return ChatHelper.GetChatUserFromCookie(request).Id;
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
        /// Sets a user offline
        /// </summary>
        /// <remarks>
        /// ToDo: This action has a hole. As anyone can just call it to make a friend 
        /// off. The good side is that, if the person is really on, he/she will automatically be back on
        /// in a few seconds.
        /// </remarks>
        public void SetUserOffline(int userId)
        {
            var roomId = this.GetMyRoomId();

            if (ChatServer.RoomExists(roomId) && ChatServer.Rooms[roomId].UserExists(userId))
                ChatServer.Rooms[roomId].SetUserOffline(userId);
        }

        [HttpGet]
        public JsonResult GetUserInfo(int userId)
        {
            var roomId = this.GetMyRoomId();
            Debug.Assert(ChatServer.RoomExists(roomId), "user room should be created when user joins the chat");

            // this will intentionally trigger an error in case the user doesn't exist.
            // the client must treat this scenario
            return this.Json(
                new
                {
                    User = ChatServer.Rooms[roomId].UserExists(userId) ? ChatServer.Rooms[roomId].UsersById[userId] : null
                },
                JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetMessageHistory(int otherUserId, long? timeStamp = null)
        {
            var roomId = this.GetMyRoomId();
            var myUserId = this.GetMyUserId(this.Request);

            Debug.Assert(ChatServer.RoomExists(roomId), "user room should be created when user joins the chat");

            // Each UserFrom Id has a LIST of messages. Of course
            // all messages have the same UserTo, of course, myUserId.
            var messages = ChatServer.Rooms[roomId].GetMessagesBetween(myUserId, otherUserId, timeStamp);

            return this.Json(new
            {
                Messages = messages,
                Timestamp = DateTime.UtcNow.Ticks.ToString()
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SendMessage(int otherUserId, string message, string clientGuid)
        {
            if (message == null) throw new ArgumentNullException("message");
            if (clientGuid == null) throw new ArgumentNullException("clientGuid");

            var roomId = this.GetMyRoomId();
            var myUserId = this.GetMyUserId(this.Request);

            Debug.Assert(ChatServer.RoomExists(roomId), "user room should be created when user joins the chat");

            if (myUserId == otherUserId)
                throw new Exception("Cannot send a message to yourself");

            ChatServer.Rooms[roomId].SendMessage(myUserId, otherUserId, message, clientGuid);

            // you may want to persist messages here
            return null;
        }


        [HttpPost]
        public JsonResult SendTypingSignal(int otherUserId)
        {
            var roomId = this.GetMyRoomId();
            var myUserId = this.GetMyUserId(this.Request);

            Debug.Assert(ChatServer.RoomExists(roomId), "user room should be created when user joins the chat");

            if (myUserId == otherUserId)
                throw new Exception("Cannot send a typing signal to yourself");

            ChatServer.Rooms[roomId].SendTypingSignal(myUserId, otherUserId);

            return null;
        }

        /// <summary>
        /// Returns the list of users in the current room
        /// </summary>
        public JsonResult GetUsersList()
        {
            var roomId = this.GetMyRoomId();
            Debug.Assert(ChatServer.RoomExists(roomId), "user room should be created when user joins the chat");

            var usersList = ChatServer.Rooms[roomId].UpdateStatusesAndGetUserList();

            return this.Json(new
            {
                Users = usersList,
            }, JsonRequestBehavior.AllowGet);
        }
    }
}