using System;

namespace AZ.TcpNet.Base
{
    /// <summary>
    /// 
    /// </summary>
    public class AzReceiveEventArgs : EventArgs
    {
        /// <summary>
        /// 
        /// </summary>
        public Guid ClientId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public AzTcpClient WorkClient { get; set; }

  
        public byte[] ReceivedData { get; set; }

        public int CmdCode { get; set; }

    }

    public class AzSocketReceiveEventArgs : EventArgs
    {
        /// <summary>
        /// 
        /// </summary>
        public Guid ClientId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public AzSocketClient WorkClient { get; set; }


        public byte[] ReceivedData { get; set; }

        public int CmdCode { get; set; }

    }
}