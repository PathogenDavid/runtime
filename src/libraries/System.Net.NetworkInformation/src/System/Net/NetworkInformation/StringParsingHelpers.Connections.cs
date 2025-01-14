// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace System.Net.NetworkInformation
{
    internal static partial class StringParsingHelpers
    {
        private static readonly string[] s_newLineSeparator = new string[] { Environment.NewLine }; // Used for string splitting

        internal static int ParseNumSocketConnections(string filePath, string protocolName)
        {
            // Parse the number of active connections out of /proc/net/sockstat
            string sockstatFile = ReadAllText(filePath);
            int indexOfTcp = sockstatFile.IndexOf(protocolName, StringComparison.Ordinal);
            int endOfTcpLine = sockstatFile.IndexOf(Environment.NewLine, indexOfTcp + 1, StringComparison.Ordinal);
            string tcpLineData = sockstatFile.Substring(indexOfTcp, endOfTcpLine - indexOfTcp);
            StringParser sockstatParser = new StringParser(tcpLineData, ' ');
            sockstatParser.MoveNextOrFail(); // Skip "<name>:"
            sockstatParser.MoveNextOrFail(); // Skip: "inuse"
            return sockstatParser.ParseNextInt32();
        }

        internal static TcpConnectionInformation[] ParseActiveTcpConnectionsFromFiles(string? tcp4ConnectionsFile, string? tcp6ConnectionsFile)
        {
            string[] v4connections;
            string[] v6connections;

            if (tcp4ConnectionsFile != null)
            {
                string tcp4FileContents = ReadAllText(tcp4ConnectionsFile);
                v4connections = tcp4FileContents.Split(s_newLineSeparator, StringSplitOptions.RemoveEmptyEntries);
            }
            else
            {
                v4connections = Array.Empty<string>();
            }

            if (tcp6ConnectionsFile != null)
            {
                string tcp6FileContents = ReadAllText(tcp6ConnectionsFile);
                v6connections = tcp6FileContents.Split(s_newLineSeparator, StringSplitOptions.RemoveEmptyEntries);
            }
            else
            {
                v6connections = Array.Empty<string>();
            }

            // First line is header in each file. On WSL, this file may be empty.
            int count = 0;
            if (v4connections.Length > 0)
            {
                count += v4connections.Length - 1;
            }

            if (v6connections.Length > 0)
            {
                count += v6connections.Length - 1;
            }

            // First line is header in each file.
            TcpConnectionInformation[] connections = new TcpConnectionInformation[count];
            int index = 0;
            int skip = 0;

            // TCP Connections
            for (int i = 1; i < v4connections.Length; i++) // Skip first line header.
            {
                string line = v4connections[i];
                connections[index] = ParseTcpConnectionInformationFromLine(line);
                if (connections[index].State == TcpState.Listen)
                {
                    skip++;
                }
                else
                {
                    index++;
                }
            }

            // TCP6 Connections
            for (int i = 1; i < v6connections.Length; i++) // Skip first line header.
            {
                string line = v6connections[i];
                connections[index] = ParseTcpConnectionInformationFromLine(line);
                if (connections[index].State == TcpState.Listen)
                {
                    skip++;
                }
                else
                {
                    index++;
                }
            }

            if (skip != 0)
            {
                Array.Resize(ref connections, connections.Length - skip);
            }

            return connections;
        }

        internal static IPEndPoint[] ParseActiveTcpListenersFromFiles(string? tcp4ConnectionsFile, string? tcp6ConnectionsFile)
        {
            string[] v4connections;
            string[] v6connections;

            if (tcp4ConnectionsFile != null)
            {
                string tcp4FileContents = ReadAllText(tcp4ConnectionsFile);
                v4connections = tcp4FileContents.Split(s_newLineSeparator, StringSplitOptions.RemoveEmptyEntries);
            }
            else
            {
                v4connections = Array.Empty<string>();
            }

            if (tcp6ConnectionsFile != null)
            {
                string tcp6FileContents = ReadAllText(tcp6ConnectionsFile);
                v6connections = tcp6FileContents.Split(s_newLineSeparator, StringSplitOptions.RemoveEmptyEntries);
            }
            else
            {
                v6connections = Array.Empty<string>();
            }

            // First line is header in each file. On WSL, this file may be empty.
            int count = 0;
            if (v4connections.Length > 0)
            {
                count += v4connections.Length - 1;
            }

            if (v6connections.Length > 0)
            {
                count += v6connections.Length - 1;
            }

            // First line is header in each file.
            IPEndPoint[] endPoints = new IPEndPoint[count];
            int index = 0;
            int skip = 0;

            // TCP Connections
            for (int i = 1; i < v4connections.Length; i++) // Skip first line header.
            {
                TcpConnectionInformation ti = ParseTcpConnectionInformationFromLine(v4connections[i]);
                if (ti.State == TcpState.Listen)
                {
                    endPoints[index] = ti.LocalEndPoint;
                    index++;
                }
                else
                {
                    skip++;
                }
            }

            // TCP6 Connections
            for (int i = 1; i < v6connections.Length; i++) // Skip first line header.
            {
                TcpConnectionInformation ti = ParseTcpConnectionInformationFromLine(v6connections[i]);
                if (ti.State == TcpState.Listen)
                {
                    endPoints[index] = ti.LocalEndPoint;
                    index++;
                }
                else
                {
                    skip++;
                }
            }

            if (skip != 0)
            {
                Array.Resize(ref endPoints, endPoints.Length - skip);
            }

            return endPoints;
        }

        public static IPEndPoint[] ParseActiveUdpListenersFromFiles(string? udp4File, string? udp6File)
        {
            string[] v4connections;
            string[] v6connections;

            if (udp4File != null)
            {
                string udp4FileContents = ReadAllText(udp4File);
                v4connections = udp4FileContents.Split(s_newLineSeparator, StringSplitOptions.RemoveEmptyEntries);
            }
            else
            {
                v4connections = Array.Empty<string>();
            }

            if (udp6File != null)
            {
                string udp6FileContents = ReadAllText(udp6File);
                v6connections = udp6FileContents.Split(s_newLineSeparator, StringSplitOptions.RemoveEmptyEntries);
            }
            else
            {
                v6connections = Array.Empty<string>();
            }

            // First line is header in each file. On WSL, this file may be empty.
            int count = 0;
            if (v4connections.Length > 0)
            {
                count += v4connections.Length - 1;
            }

            if (v6connections.Length > 0)
            {
                count += v6connections.Length - 1;
            }

            IPEndPoint[] endPoints = new IPEndPoint[count];
            int index = 0;

            // UDP Connections
            for (int i = 1; i < v4connections.Length; i++) // Skip first line header.
            {
                string line = v4connections[i];
                IPEndPoint endPoint = ParseLocalConnectionInformation(line);
                endPoints[index++] = endPoint;
            }

            // UDP6 Connections
            for (int i = 1; i < v6connections.Length; i++) // Skip first line header.
            {
                string line = v6connections[i];
                IPEndPoint endPoint = ParseLocalConnectionInformation(line);
                endPoints[index++] = endPoint;
            }

            return endPoints;
        }

        // Parsing logic for local and remote addresses and ports, as well as socket state.
        internal static TcpConnectionInformation ParseTcpConnectionInformationFromLine(string line)
        {
            StringParser parser = new StringParser(line, ' ', skipEmpty: true);
            parser.MoveNextOrFail(); // skip Index

            string localAddressAndPort = parser.MoveAndExtractNext(); // local_address
            IPEndPoint localEndPoint = ParseAddressAndPort(localAddressAndPort);

            string remoteAddressAndPort = parser.MoveAndExtractNext(); // rem_address
            IPEndPoint remoteEndPoint = ParseAddressAndPort(remoteAddressAndPort);

            string socketStateHex = parser.MoveAndExtractNext();
            int nativeTcpState;
            if (!int.TryParse(socketStateHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out nativeTcpState))
            {
                throw ExceptionHelper.CreateForParseFailure();
            }

            TcpState tcpState = MapTcpState(nativeTcpState);

            return new SimpleTcpConnectionInformation(localEndPoint, remoteEndPoint, tcpState);
        }

        // Common parsing logic for the local connection information.
        private static IPEndPoint ParseLocalConnectionInformation(string line)
        {
            StringParser parser = new StringParser(line, ' ', skipEmpty: true);
            parser.MoveNextOrFail(); // skip Index

            string localAddressAndPort = parser.MoveAndExtractNext();
            int indexOfColon = localAddressAndPort.IndexOf(':');
            if (indexOfColon == -1)
            {
                throw ExceptionHelper.CreateForParseFailure();
            }

            IPAddress localIPAddress = ParseHexIPAddress(localAddressAndPort.AsSpan(0, indexOfColon));

            ReadOnlySpan<char> portSpan = localAddressAndPort.AsSpan(indexOfColon + 1, localAddressAndPort.Length - (indexOfColon + 1));
            int localPort;
            if (!int.TryParse(portSpan, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out localPort))
            {
                throw ExceptionHelper.CreateForParseFailure();
            }

            return new IPEndPoint(localIPAddress, localPort);
        }

        private static IPEndPoint ParseAddressAndPort(string colonSeparatedAddress)
        {
            int indexOfColon = colonSeparatedAddress.IndexOf(':');
            if (indexOfColon == -1)
            {
                throw ExceptionHelper.CreateForParseFailure();
            }

            IPAddress ipAddress = ParseHexIPAddress(colonSeparatedAddress.AsSpan(0, indexOfColon));

            ReadOnlySpan<char> portSpan = colonSeparatedAddress.AsSpan(indexOfColon + 1, colonSeparatedAddress.Length - (indexOfColon + 1));
            int port;
            if (!int.TryParse(portSpan, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out port))
            {
                throw ExceptionHelper.CreateForParseFailure();
            }

            return new IPEndPoint(ipAddress, port);
        }

        // Maps from Linux TCP states to .NET TcpStates.
        private static TcpState MapTcpState(int state)
        {
            return Interop.Sys.MapTcpState((int)state);
        }

        internal static IPAddress ParseHexIPAddress(ReadOnlySpan<char> remoteAddressString)
        {
            if (remoteAddressString.Length <= 8) // IPv4 Address
            {
                return ParseIPv4HexString(remoteAddressString);
            }
            else if (remoteAddressString.Length == 32) // IPv6 Address
            {
                return ParseIPv6HexString(remoteAddressString);
            }
            else
            {
                throw ExceptionHelper.CreateForParseFailure();
            }
        }

        // Simply converts the hex string into a long and uses the IPAddress(long) constructor.
        // Strings passed to this method must be 8 or less characters in length (32-bit address).
        private static IPAddress ParseIPv4HexString(ReadOnlySpan<char> hexAddress)
        {
            IPAddress ipAddress;
            long addressValue;
            if (!long.TryParse(hexAddress, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out addressValue))
            {
                throw ExceptionHelper.CreateForParseFailure();
            }

            ipAddress = new IPAddress(addressValue);
            return ipAddress;
        }

        // Parses a 128-bit IPv6 Address stored as 4 concatenated 32-bit hex numbers.
        // If isSequence is true it assumes that hexAddress is in sequence of IPv6 bytes.
        // First number corresponds to lower address part
        // E.g. IP-address:                           fe80::215:5dff:fe00:402
        //      It's bytes in direct order:           FE-80-00-00  00-00-00-00  02-15-5D-FF  FE-00-04-02
        //      It's representation in /proc/net/tcp6: 00-00-80-FE  00-00-00-00  FF-5D-15-02  02-04-00-FE
        //                                             (dashes and spaces added above for readability)
        // Strings passed to this must be 32 characters in length.
        private static IPAddress ParseIPv6HexString(ReadOnlySpan<char> hexAddress, bool isNetworkOrder = false)
        {
            Debug.Assert(hexAddress.Length == 32);
            Span<byte> addressBytes = stackalloc byte[16];
            if (isNetworkOrder || !BitConverter.IsLittleEndian)
            {
                for (int i = 0; i < 16; i++)
                {
                    addressBytes[i] = (byte)(HexToByte(hexAddress[(i * 2)]) * 16
                                           + HexToByte(hexAddress[(i * 2) + 1]));
                }
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        int srcIndex = i * 4 + 3 - j;
                        int targetIndex = i * 4 + j;
                        addressBytes[targetIndex] = (byte)(HexToByte(hexAddress[srcIndex * 2]) * 16
                                                         + HexToByte(hexAddress[srcIndex * 2 + 1]));
                    }
                }
            }

            IPAddress ipAddress = new IPAddress(addressBytes);
            return ipAddress;
        }

        private static byte HexToByte(char val)
        {
            int result = HexConverter.FromChar(val);
            if (result == 0xFF)
            {
                throw ExceptionHelper.CreateForParseFailure();
            }

            return (byte)result;
        }
    }
}
