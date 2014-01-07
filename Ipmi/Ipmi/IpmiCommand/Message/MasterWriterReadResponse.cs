﻿/********************************************************
*                                                       *
*   Copyright (C) Microsoft. All rights reserved.       *
*                                                       *
*   Auther:  Bryankel@Microsoft.com                     *
*   							                        *
********************************************************/

namespace Microsoft.GFS.WCS.ChassisManager.Ipmi
{
    /// <summary>
    /// Represents the IPMI 'Send Master Write-Read' application response message.
    /// </summary>
    [IpmiMessageRequest(IpmiFunctions.Application, IpmiCommand.MasterReadWrite)]
    internal class MasterWriteReadResponse : IpmiResponse
    {

        /// <summary>
        /// Response message payload.
        /// </summary>
        private byte[] messageData;

        /// <summary>
        /// Response message payload.
        /// </summary>
        [IpmiMessageData(0)]
        public byte[] MessageData
        {
            get { return this.messageData; }
            set { this.messageData = value; }
        }
    }
}
