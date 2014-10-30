// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved
// Licensed under the Apache License, Version 2.0 (the "License"); 
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at 
// http://www.apache.org/licenses/LICENSE-2.0 

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR
// CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT. 
// See the Apache 2 License for the specific language governing permissions and limitations under the License. 

namespace Microsoft.GFS.WCS.ChassisManager.Ipmi
{

    /// <summary>
    /// Represents the IPMI 'Get Memory Info Request' OEM request message.
    /// </summary>
    [IpmiMessageRequest(IpmiFunctions.OemGroup, IpmiCommand.GetMemoryInfo)]
    internal class GetMemoryInfoRequest : IpmiRequest
    {

        /// <summary>
        /// Processor device index.  Default = 0x01
        /// </summary>
        private readonly byte _dimm;

        /// <summary>
        /// Get Memory Info Request.  Index 1 based.
        /// </summary>
        internal GetMemoryInfoRequest(byte dimm)
        { this._dimm = (dimm == 0 ? (byte)1 : dimm); }

        /// <summary>
        /// Get Memory Presence Info, 0x00 alters the return type.
        /// </summary>
        protected GetMemoryInfoRequest()
        { this._dimm = 0x00; }


        /// <summary>
        /// DIMM Number
        /// </summary>       
        [IpmiMessageData(0)]
        public byte DIMM
        {
            get { return this._dimm; }

        }
    }

    /// <summary>
    /// Represents the IPMI 'Get Memory Index Request' OEM request message.
    /// </summary>
    [IpmiMessageRequest(IpmiFunctions.OemGroup, IpmiCommand.GetMemoryInfo)]
    internal class GetMemoryIndexRequest : GetMemoryInfoRequest
    {
        internal GetMemoryIndexRequest() : base()
        { 
        }
    }

}
