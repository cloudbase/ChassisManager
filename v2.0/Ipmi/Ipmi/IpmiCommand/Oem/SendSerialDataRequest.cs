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
    /// Represents the IPMI 'Send Serial Data' OEM request message.
    /// </summary>
    [IpmiMessageRequest(IpmiFunctions.OemGroup, IpmiCommand.SendSerialData)]
    internal class SendSerialDataRequest : IpmiRequest
    {
        /// <summary>
        /// buffer Index
        /// </summary>
        private readonly ushort payloadLength;

        private readonly byte[] payload;


        /// <summary>
        /// Initialize instance of the class.
        /// </summary>
        /// <param name="length">Payload Length</param>
        /// <param name="payload">Payload Length</param>
        /// </summary>  
        internal SendSerialDataRequest(ushort length, byte[] payload)
        {
            this.payloadLength = length;
            this.payload = payload;
        }

        /// <summary>
        /// Payload Length
        /// </summary>
        [IpmiMessageData(0)]
        public ushort PayloadLength
        {
            get { return this.payloadLength; }

        }

        /// <summary>
        /// Payload
        /// </summary>
        [IpmiMessageData(2)]
        public byte[] Payload
        {
            get { return this.payload; }

        }

    }
}
