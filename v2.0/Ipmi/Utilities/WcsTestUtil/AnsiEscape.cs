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

namespace WcsTestUtil
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Diagnostics;
    using System.Threading;
    using Microsoft.GFS.WCS.ChassisManager.Ipmi;

    class AnsiEscape : Vt100Base
    {
        /// <summary>
        /// IPMI Serial Client to send packaged payload.
        /// </summary>
        internal IpmiSerialClient IpmiClient
        {
             get;
             private set;
        }

        /// <summary>
        /// Wait time after sending a payload chunk to the remote 
        /// console. 180ms = default
        /// </summary>
        private const int _chunkwait = 180;

        /// <summary>
        /// Cursor position tracker
        /// </summary>
        private int _posTracker = 0;

        /// <summary>
        /// Start position tracker, used in conjunction
        /// with cursor position tracker
        /// </summary>
        private volatile bool _start = false;

        /// <summary>
        /// Console payload, in string format.
        /// </summary>
        private string scrData = string.Empty;

        // capture console buffer hight
        // during initialization
        private int bufferHeight = Console.BufferHeight;

        // capture console buffer hight
        // during initialization
        private int bufferWidth = Console.BufferWidth;

        /// <summary>
        /// VT100 codepage.
        /// </summary>
        private uint codepage = 437;

        /// <summary>
        /// Determines stop console method
        /// </summary>
        private SerialConsoleType consoleType;
     
        /// <summary>
        /// Initializes class and sets defaults.
        /// </summary>
        internal AnsiEscape(IpmiSerialClient sc, SerialConsoleType consoleType)
        {
            // Clear the Console
            Console.Clear();

            // set console and buffer size
            base.SetConsoleSize(80, 25);
            base.SetConsoleBufferSize(80, 25);

            // Ipmi Client
            this.IpmiClient = sc;

            // Set the Console Type
            this.consoleType = consoleType;

            // set cursor positions to begining
            PositionLeft = 0;
            PositionTop = 0;

            // set cursor position to default
            base.SetCursorPosition(PositionLeft, PositionTop);

            // capture current code page.
            uint cp = NativeMethods.GetCodePage();
            if (cp > 0)
                codepage = cp;
           
            // Set the Console Code page to 437
            NativeMethods.SetCodePage(437);

            // pInvoke Call to Disable Console
            // wordwrap.  By default VT100 does not
            // expect wordwarp
            NativeMethods.DisableWordWrap(); // Disable Word Wrap

            // Capture Ctrl+C as key command.
            Console.TreatControlCAsInput = true;

            // enable receive loop.
            SharedFunc.SetSerialSession(true);

        }

        /// <summary>
        /// Rerverts Console back to original state.
        /// </summary>
        internal void RevertConsole()
        {
            // set page code back to original
            NativeMethods.SetCodePage(codepage);

            // re-enable word-wrap
            NativeMethods.EnableWordWrap(); // Enable Word Wrap

            // Disable Ctrl+C capture, allow it termiante.
            Console.TreatControlCAsInput = false;

            // set console buffer size back to original state.
            base.SetConsoleBufferSize(bufferWidth, bufferHeight);

            // Issue a graceful terminate.
            SharedFunc.SetSerialSession(false);
        }

        /// <summary>
        /// Designed to run continously reading for user
        /// input.  The method intercepts user input
        /// </summary>
        internal void ReadConsoleInput()
        {

            byte CompletionCode;

            //ConsoleKeyInfo keyInf;
            while (SharedFunc.ActiveSerialSession)
            {
                // read key and intercept
                ConsoleKeyInfo keyInf = Console.ReadKey(true);

                if (!IsFunctionKey(keyInf))
                {
                    if (!_start)
                    { 
                        _posTracker = Console.CursorLeft;
                        _start = true;
                    }

                    if (keyInf.Key == ConsoleKey.Backspace)
                    {
                        if (scrData.Length > 0)
                        {
                            scrData = scrData.Remove((scrData.Length - 1));
                            Console.Write(keyInf.KeyChar);
                            Console.Write(" ");
                            Console.CursorLeft = (Console.CursorLeft - 1);
                        }
                    }
                    else
                    {
                        scrData = scrData + keyInf.KeyChar;
                        Console.Write(keyInf.KeyChar);
                    }
                }
                else
                {
                    lock (SharedFunc.Locker)
                    {
                        SendConsoleKeyData(keyInf, IpmiUtil.enterEncodeCRLF, out CompletionCode);
                    }

                    Thread.Sleep(100);

                    if (CompletionCode != 0)
                    {
                        Console.WriteLine("Serial Session Send Failed with Completion Code: {0}", CompletionCode);
                        Tracer.WriteError("Serial Session Send Failed with Completion Code: {0}", CompletionCode);
                        SharedFunc.SetSerialSession(false);
                    }
                    
                }
            }
        }



        /// <summary>
        /// Check ConsoleKeyInfo for VT100 Function Key
        /// </summary>
        private bool IsFunctionKey(ConsoleKeyInfo keyInfo)
        {
            if ((keyInfo.Key >= ConsoleKey.F1 // if key between F1 & F12
                && keyInfo.Key <= ConsoleKey.F12) ||
                (keyInfo.Key >= ConsoleKey.LeftArrow // if key is Arrow
                && keyInfo.Key <= ConsoleKey.DownArrow) ||
                (keyInfo.Key == ConsoleKey.Enter) || // if key is Enter
                (keyInfo.Key == ConsoleKey.Escape) || // if key is Escape
                (keyInfo.Key == ConsoleKey.Delete) || // if key is Delete
                (keyInfo.Key == ConsoleKey.Tab) || // if key is Tab
                (keyInfo.Key == ConsoleKey.C &&
                 keyInfo.Modifiers == ConsoleModifiers.Control) // Ctrl + C
                )
            {
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Send encodeded payload
        /// enterEncodeCRLF: VT100 encoding for Enter key. True: CR+LF for Enter. False: CR for Enter.
        /// </summary>
        private void SendConsoleKeyData(ConsoleKeyInfo keyInfo, bool enterEncodeCRLF, out byte CompletionCode)
        {
            byte[] payload = Vt100Encode(keyInfo, enterEncodeCRLF);

            if (keyInfo.Key == ConsoleKey.Enter)
            {
                if (_start)
                {
                    Console.CursorLeft = _posTracker;
                    _start = false;
                }
            }

            if (consoleType == SerialConsoleType.Muxed)
            {
                // Note:  AMI BIOS has a 10 byte receive buffer.  If more than 10 bytes
                //        are sent in a single payload the result maybe truncated.

                // create 3 integers for indexing the array.
                int index = 0, chunksize = 10, length = payload.Length;

                // device the payload array into equal chunk sizes, then send the 
                // remaining trailing bytes.
                while (index < length)
                {
                    if (index + chunksize > length)
                        chunksize = (length - index);

                    byte[] chunk = new byte[chunksize];

                    Buffer.BlockCopy(payload, index, chunk, 0, chunksize);

                    // send the payload chunks 1 at a time.  This prevents
                    // buffer overrun on the command interpreter of the AMI
                    // BIOS
                    IpmiClient.SerialWrite(payload);

                    // wait after each chunk is sent, the command interpreter
                    // must manually process each charactor.
                    Thread.Sleep(_chunkwait);

                    // increase the index
                    index += chunksize;
                }

                CompletionCode = 0x00;
            }
            else
            {
                // Send the Ipmi packaged payload.
                SendSerialData send = IpmiClient.SendSerialData((ushort)payload.Length, payload);

                CompletionCode = send.CompletionCode;
            }

        }


        /// <summary>
        /// Encode VT100 escape sequences into function key
        /// enterEncodeCRLF: VT100 encoding for Enter key. True: CR+LF for Enter. False: CR for Enter.
        /// </summary>
        private byte[] Vt100Encode(ConsoleKeyInfo keyInfo, bool enterEncodeCRLF)
        {
            byte[] enc = new byte[3];
            enc[0] = 0x1B; // Esc

            if (keyInfo.Key >= ConsoleKey.F1 // if key between F1 & F4
                && keyInfo.Key <= ConsoleKey.F4)
            {
                enc[1] = 0x4F; // O
                switch (keyInfo.Key)
                {
                    case ConsoleKey.F1:
                        enc[2] = 0x50; // P
                        break;
                    case ConsoleKey.F2:
                        enc[2] = 0x51; // Q
                        break;
                    case ConsoleKey.F3:
                        enc[2] = 0x52; // R
                        break;
                    case ConsoleKey.F4:
                        enc[2] = 0x53; // S
                        break;
                    default:
                        break;
                }
            }
            else if (keyInfo.Key >= ConsoleKey.F5 // if key between F5 & F12
                && keyInfo.Key <= ConsoleKey.F12)
            {
                enc = new byte[2];
                enc[0] = 0x1B; // Esc

                switch (keyInfo.Key)
                {
                    case ConsoleKey.F5:
                        enc[1] = 0x35;
                        break;
                    case ConsoleKey.F6:
                        enc[1] = 0x36;
                        break;
                    case ConsoleKey.F7:
                        enc[1] = 0x37;
                        break;
                    case ConsoleKey.F8:
                        enc[1] = 0x38;
                        break;
                    case ConsoleKey.F9:
                        enc[1] = 0x39;
                        break;
                    case ConsoleKey.F10:
                        enc[1] = 0x30;
                        break;
                    case ConsoleKey.F11:
                        enc[1] = 0x2A;
                        break;
                    case ConsoleKey.F12:
                        enc[1] = 0x28;
                        break;
                    default:
                        break;
                }
            }
            else if (keyInfo.Key >= ConsoleKey.LeftArrow // if key is Arrow
                    && keyInfo.Key <= ConsoleKey.DownArrow)
            {
                enc[1] = 0x5B; // bracket

                switch (keyInfo.Key)
                {
                    case ConsoleKey.UpArrow:
                        enc[2] = 0x41; // A
                        break;
                    case ConsoleKey.DownArrow:
                        enc[2] = 0x42; // B
                        break;
                    case ConsoleKey.RightArrow:
                        enc[2] = 0x43; // C
                        break;
                    case ConsoleKey.LeftArrow:
                        enc[2] = 0x44; // D
                        break;
                    default:
                        break;
                }

            }
            else if (keyInfo.Key == ConsoleKey.Enter) // if key is Enter
            {
                byte enc_length;

                if (enterEncodeCRLF)
                {
                    enc_length = 2;
                    enc = new byte[2] { 0x0D, 0x0A };
                }
                else
                {
                    enc_length = 1;
                    enc = new byte[1] { 0x0D };
                }

                if (scrData != string.Empty && scrData.Length > 0)
                {
                    // get screen data bytes
                    byte[] scrPayload = Encoding.UTF8.GetBytes(scrData);

                    // flush screen data
                    scrData = string.Empty;

                    // create new serialized packet with screen bytes and return payload
                    enc = new byte[(scrPayload.Length + enc_length)];

                    Buffer.BlockCopy(scrPayload, 0, enc, 0, scrPayload.Length);

                    // Add return key
                    enc[scrPayload.Length] = 0x0D;
                    if (enterEncodeCRLF)
                    {
                        enc[(scrPayload.Length + 1)] = 0x0A;
                    }
                }
            }
            else if (keyInfo.Key == ConsoleKey.Delete)
            {
                // ^[3~"
                enc = new byte[4];
                enc[0] = 0x1B;
                enc[1] = 0x5B; // bracket
                enc[2] = 0x33;
                enc[3] = 0x7E;
            }
            else if (keyInfo.Key == ConsoleKey.Escape) // Escape
            {
                enc = new byte[1];
                enc[0] = 0x1B;
            }
            else if (keyInfo.Key == ConsoleKey.C &&
                 keyInfo.Modifiers == ConsoleModifiers.Control) // Ctrl + C
            {
                // Issue a graceful terminate.
                SharedFunc.SetSerialSession(false);

                // Clear console window
                Console.Clear();
            }
            else if (keyInfo.Key == ConsoleKey.Tab) // TAB
            {
                enc = new byte[1];
                enc[0] = 0x09;
            }

            return enc;
        }
    }


}
