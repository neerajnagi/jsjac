using System;

namespace ChatJs.Net
{
    /// <summary>
    /// Information about a chat user
    /// </summary>
    public class ChatUser
    {
        /// <summary>
        /// User chat status. For now, it only supports online and offline
        /// </summary>
        public enum StatusType
        {
            Offline = 0,
            Online = 1
        }

        public ChatUser()
        {
            this.Status = StatusType.Offline;
        }

        /// <summary>
        /// User Id (preferebly the same as database user Id)
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// User display name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Profile Url
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// User profile picture URL (Gravatar, for instance)
        /// </summary>
        public string ProfilePictureUrl { get; set; }

        /// <summary>
        /// User's status
        /// </summary>
        public StatusType Status { get; set; }

        /// <summary>
        /// Last time this user has been active
        /// </summary>
        public DateTime LastActiveOn { get; set; }

        /// <summary>
        /// User e-mail
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// User room id
        /// </summary>
        public string RoomId { get; set; }
    }
}