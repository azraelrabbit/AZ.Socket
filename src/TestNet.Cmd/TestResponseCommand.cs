using System;

namespace TestNet.Command
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class TestResponseCommand : AzCommandBase
    {
        /// <summary>
        /// 
        /// </summary>
        public TestResponseCommand()
        {
            Code = (int)AzCommandType.TestResponse;
        }

        /// <summary>
        /// 
        /// </summary>
        public string TestValue { get; set; }

        public DateTime ReqTime { get; set; }
    }
}
