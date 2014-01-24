using System.Collections.Generic;
using System.Web.Mvc;

namespace ChatJsMvcSample.Code.LongPolling
{
    public abstract class LongPollingProvider
    {
        public const int WAIT_TIMEOUT = 30000;

        /// <summary>
        /// Initializes the long polling provider
        /// </summary>
        public abstract void Initialize();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="myUserId"></param>
        /// <param name="roomIdceId"></param>
        /// <param name="timestamp"></param>
        /// <param name="connectionString"></param>
        /// <param name="controller"></param>
        public abstract IEnumerable<LongPollingEvent> WaitForEvents(int myUserId, string roomId, long timestamp, string connectionString, Controller controller);
    }
}
