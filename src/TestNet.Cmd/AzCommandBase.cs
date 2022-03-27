using System;
using System.Collections.Generic;
using System.Linq;
using AZ.TcpNet.Base;

namespace TestNet.Command
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public abstract class AzCommandBase
    {
        //public 
        /// <summary>
        /// 消息类型
        /// </summary>
        public int Code { get; set; }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="code"></param>
        //public AzCommandBase(int code)
        //{
        //    Code = code;
        //}

        /// <summary>
        /// 
        /// </summary>
        public object Content { get; set; }

        /// <summary>
        /// 将协议序列化为Byte[]
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
            
        {
            var reqBuffer = Helper.Serialize(this);
            var buffer = new List<byte>();
            var bodyLengthbuffer = BitConverter.GetBytes(reqBuffer.Length);

            var cmdBuf = BitConverter.GetBytes(Code);

            buffer.AddRange(bodyLengthbuffer);
            buffer.AddRange(cmdBuf);
            buffer.AddRange(reqBuffer);

            return buffer.ToArray();
        }

        /// <summary>
        /// 转换到具体指令
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T ToCommand<T>() where T : AzCommandBase
        {
            return this as T;
        }

        /// <summary>
        /// 反序列化到指令实体
        /// </summary>
        /// <param name="buffer"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T FromBytes<T>(byte[] buffer) where T : AzCommandBase
        {
            //try
            //{
            return Helper.Deserialize(buffer.ToArray()) as T;
            //}
            //catch (Exception ex)
            //{
            //    LogHelper.Error(ex);
            //}
            //return null;
        }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="commandBase"></param>
        ///// <param name="client"></param>
        ///// <param name="argument"></param>
        //public virtual void Process(AzCommandBase commandBase, AzTcpClient client = null, object argument = null)
        //{
        //}
    }
}