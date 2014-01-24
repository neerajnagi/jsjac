using System;
using System.Web.Mvc;
using ChatJs.Net;
using ChatJsMvcSample.Code;
using ChatJsMvcSample.Code.LongPolling;
using ChatJsMvcSample.Code.LongPolling.Chat;
using ChatJsMvcSample.Code.SignalR;
using ChatJsMvcSample.Models.ViewModels;

namespace ChatJsMvcSample.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            var existingUser = ChatHelper.GetChatUserFromCookie(this.Request);
            ChatViewModel chatViewModel = null;
            if (existingUser != null)
            {
                if (!ChatHub.IsUserRegistered(existingUser))
                {
                    // cookie is invalid
                    ChatHelper.RemoveCookie(this.Response);
                    return this.RedirectToAction("Index");
                }

                // in this case the authentication cookie is valid and we must render the chat
                chatViewModel = new ChatViewModel()
                    {
                        IsUserAuthenticated = true,
                        UserId = existingUser.Id,
                        UserName = existingUser.Name,
                        UserProfilePictureUrl = existingUser.ProfilePictureUrl
                    };
            }

            return this.View(chatViewModel);
        }

        /// <summary>
        /// Joins the chat
        /// </summary>
        public ActionResult JoinChat(string userName, string email)
        {
            // try to find an existing user with the same e-mail
            var user = ChatHub.FindUserByEmail(email);

            if (user == null)
            {
                user = new ChatUser()
                    {
                        Name = userName,
                        Email = email,
                        ProfilePictureUrl = GravatarHelper.GetGravatarUrl(GravatarHelper.GetGravatarHash(email), GravatarHelper.Size.s32),
                        Id = new Random().Next(100000),
                        Status = ChatUser.StatusType.Online,
                        RoomId = ChatController.ROOM_ID_STUB
                    };

                // for signalr
                {
                    ChatHub.RegisterNewUser(user);
                }

                // for long-polling
                {
                    ChatServer.SetupRoomIfNonexisting(ChatController.ROOM_ID_STUB);
                    ChatServer.Rooms[ChatController.ROOM_ID_STUB].RegisterNewUser(user);
                }
            }

            // Normally it wouldn't be necessary to create this cookie because the
            // FormsAuthentication cookie does this
            ChatHelper.CreateNewUserCookie(this.Response, user);

            return this.RedirectToAction("Index");
        }

        /// <summary>
        /// Leaves the chat
        /// </summary>
        public ActionResult LeaveChat(string userName, string email)
        {
            ChatHelper.RemoveCookie(this.Response);
            return this.RedirectToAction("Index");
        }
    }
}