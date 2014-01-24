﻿using System;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using ChatJs.Net;
using Microsoft.AspNet.SignalR;

namespace ChatJsMvcSample.Code
{
    /// <summary>
    /// Stub methods for obtaining the db user from the cookie.
    /// In a normal situation this would be done using the forms authentication cookie
    /// </summary>
    public class ChatHelper
    {
        public static string COOKIE_NAME = "chatjs";

        /// <summary>
        /// Returns information about the user from cookie
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static ChatUser GetChatUserFromCookie(HttpRequestBase request)
        {
            if (request == null) throw new ArgumentNullException("request");
            var cookie = request.Cookies[COOKIE_NAME];
            if (cookie == null) return null;

            var cookieBytes = Convert.FromBase64String(cookie.Value);
            var cookieString = Encoding.UTF8.GetString(cookieBytes);
            return new JavaScriptSerializer().Deserialize<ChatUser>(cookieString);
        }

        /// <summary>
        /// Returns information about the user from cookie
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static ChatUser GetChatUserFromCookie(IRequest request)
        {
            if (request == null) throw new ArgumentNullException("request");
            var cookie = request.Cookies[COOKIE_NAME];

            if (cookie == null)
                return null;

            var cookieBytes = Convert.FromBase64String(cookie.Value);
            var cookieString = Encoding.UTF8.GetString(cookieBytes);
            return new JavaScriptSerializer().Deserialize<ChatUser>(cookieString);
        }

        /// <summary>
        /// Removes the cookie. Probably because it's invalid
        /// </summary>
        /// <param name="response"></param>
        public static void RemoveCookie(HttpResponseBase response)
        {
            if (response == null) throw new ArgumentNullException("response");
            var cookie = response.Cookies[COOKIE_NAME];
            if (cookie != null)
                cookie.Expires = DateTime.Now.AddDays(-1); 
        }

        /// <summary>
        /// Creates a new cookie with information about the user
        /// </summary>
        /// <param name="request"></param>
        /// <param name="chatUser"></param>
        public static void CreateNewUserCookie(HttpResponseBase request, ChatUser chatUser)
        {
            if (request == null) throw new ArgumentNullException("request");
            if (chatUser == null) throw new ArgumentNullException("chatUser");

            var cookie = new HttpCookie(COOKIE_NAME)
                {
                    Value = Convert.ToBase64String(Encoding.UTF8.GetBytes(new JavaScriptSerializer().Serialize(chatUser))),
                    Expires = DateTime.UtcNow.AddDays(30)
                };
            request.Cookies.Add(cookie);
        }
    }
}