using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Snap7;
using System.Threading;

namespace Snap7Server
{
    class Program
    {
        static S7Server Srv;
        static S7Server.USrvEvent Event;
        static private byte[] DB1 = new byte[256];  // Our DB1
        private static S7Server.TSrvCallback TheEventCallBack; // <== Static var containig the callback
        private static S7Server.TSrvCallback TheReadCallBack; // <== Static var containig the callback
       
        static void EventCallback(IntPtr usrPtr, ref S7Server.USrvEvent Event, int Size)
        {
            Console.WriteLine("Event Callback ");
            if (Event.EvtCode.Equals(S7Server.evcDataRead))
            {
                Console.WriteLine("Sent data to client.");
            }
            if (Event.EvtCode.Equals(S7Server.evcDataWrite))
            {
                Console.WriteLine("Received data from client.");
                HexDump(DB1, DB1.Length);
            }
        }

        static void ReadEventCallback(IntPtr usrPtr, ref S7Server.USrvEvent Event, int Size)
        {
            Console.WriteLine("ReadEvent Callback");
        }


        static void HexDump(byte[] bytes, int Size)
        {
            if (bytes == null)
                return;
            int bytesLength = Size;
            int bytesPerLine = 16;

            char[] HexChars = "0123456789ABCDEF".ToCharArray();

            int firstHexColumn =
                  8                   // 8 characters for the address
                + 3;                  // 3 spaces

            int firstCharColumn = firstHexColumn
                + bytesPerLine * 3       // - 2 digit for the hexadecimal value and 1 space
                + (bytesPerLine - 1) / 8 // - 1 extra space every 8 characters from the 9th
                + 2;                  // 2 spaces 

            int lineLength = firstCharColumn
                + bytesPerLine           // - characters to show the ascii value
                + Environment.NewLine.Length; // Carriage return and line feed (should normally be 2)

            char[] line = (new String(' ', lineLength - 2) + Environment.NewLine).ToCharArray();
            int expectedLines = (bytesLength + bytesPerLine - 1) / bytesPerLine;
            StringBuilder result = new StringBuilder(expectedLines * lineLength);

            for (int i = 0; i < bytesLength; i += bytesPerLine)
            {
                line[0] = HexChars[(i >> 28) & 0xF];
                line[1] = HexChars[(i >> 24) & 0xF];
                line[2] = HexChars[(i >> 20) & 0xF];
                line[3] = HexChars[(i >> 16) & 0xF];
                line[4] = HexChars[(i >> 12) & 0xF];
                line[5] = HexChars[(i >> 8) & 0xF];
                line[6] = HexChars[(i >> 4) & 0xF];
                line[7] = HexChars[(i >> 0) & 0xF];

                int hexColumn = firstHexColumn;
                int charColumn = firstCharColumn;

                for (int j = 0; j < bytesPerLine; j++)
                {
                    if (j > 0 && (j & 7) == 0) hexColumn++;
                    if (i + j >= bytesLength)
                    {
                        line[hexColumn] = ' ';
                        line[hexColumn + 1] = ' ';
                        line[charColumn] = ' ';
                    }
                    else
                    {
                        byte b = bytes[i + j];
                        line[hexColumn] = HexChars[(b >> 4) & 0xF];
                        line[hexColumn + 1] = HexChars[b & 0xF];
                        line[charColumn] = (b < 32 ? '·' : (char)b);
                    }
                    hexColumn += 3;
                    charColumn++;
                }
                result.Append(line);
            }
            Console.WriteLine(result.ToString());
        }

        static void Main(string[] args)
        {

            System.Console.Out.WriteLine("S7 Server starting up");
            Event = new S7Server.USrvEvent();
            Srv = new S7Server();
            Srv.RegisterArea(S7Server.srvAreaDB,  // We are registering a DB
                               1,                   // Its number is 1 (DB1)
                               DB1,                 // Our buffer for DB1
                               DB1.Length);         // Its size

            TheEventCallBack = new S7Server.TSrvCallback(EventCallback);
            TheReadCallBack = new S7Server.TSrvCallback(ReadEventCallback);
            Srv.SetEventsCallBack(TheEventCallBack, IntPtr.Zero);
            Srv.SetReadEventsCallBack(TheReadCallBack, IntPtr.Zero);
            
            Srv.EventMask = Srv.EventMask & 1; // Mask 00000000
            Srv.EventMask = Srv.EventMask | S7Server.evcDataRead;
            Srv.EventMask = Srv.EventMask | S7Server.evcDataWrite;

            int Error = Srv.Start();
            if (Error == 0)
            {
                Console.WriteLine("Press ESC to stop");
                do
                {
                    while (!Console.KeyAvailable)
                    {
                        while (Srv.PickEvent(ref Event)){
                            Console.Out.WriteLine(Srv.EventText(ref Event));
                        }
                    }
                } while (Console.ReadKey(true).Key != ConsoleKey.Escape);
                Srv.Stop();
            }

            System.Console.Out.WriteLine("Enter any key to quit");
            System.Console.ReadKey();
        }
    }
}
