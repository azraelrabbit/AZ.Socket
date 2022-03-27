using System;

namespace TestNet.Command
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable] 
    public class TestRequestCommand : AzCommandBase
    {
        /// <summary>
        /// 
        /// </summary>
        public TestRequestCommand( ) 
        {
            Code = (int)AzCommandType.TestRequest;
        }

        /// <summary>
        /// 
        /// </summary>
        public string TestValue { get; set; }

        public DateTime ReqTime { get; set; }
    }
}
