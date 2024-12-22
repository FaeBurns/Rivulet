using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Network
{
    public class ServerSessionCommunicator : IDisposable
    {
        private readonly SessionManager m_sessionManager;
        private TcpListener m_server;

        public ServerSessionCommunicator(SessionManager sessionManager)
        {
            m_sessionManager = sessionManager;
        }

        public void StartServer(int port)
        {
            m_server = new TcpListener(IPAddress.Any, port);
            m_server.Start();
        }

        public async Task AcceptOneConnectionAsync()
        {
            EnsureServer();

            TcpClient tcpClient = await m_server.AcceptTcpClientAsync();
            Thread thread = new Thread(() => ClientConnectionThread(tcpClient));
            thread.Start();
        }

        public void Dispose()
        {
            m_server.Stop();
        }

        private void ClientConnectionThread(TcpClient tcpClient)
        {
            NetworkStream stream = tcpClient.GetStream();

            Span<byte> buffer = stackalloc byte[4];
            while (!Application.exitCancellationToken.IsCancellationRequested)
            {
                ReadExactly(stream, buffer);
                int commandRead = BitConverter.ToInt32(buffer);

                switch ((CommandType)(byte)commandRead)
                {
                    case CommandType.RESERVED:
                        Debug.LogError("Received RESERVED command");
                        break;
                    case CommandType.NEGOTIATE_STREAM:
                        NegotiateStream(stream);
                        break;
                    case CommandType.EXIT:
                        ExitStream();
                        break;
                    default:
                        Debug.LogError($"Received {(byte)commandRead}");
                        throw new IOException("Previous read failed to consume entire buffer or command was unknown.");
                }
            }
        }

        /// <summary>
        /// Reads 4x3 bytes from the stream.
        /// </summary>
        /// <param name="stream"></param>
        private void NegotiateStream(NetworkStream stream)
        {
            Debug.Log("Received request from server to negotiate stream");
            using StreamWriter sw = new StreamWriter(stream);
            using StreamReader sr = new StreamReader(stream);

            int width;
            int height;
            float framerate;

            Span<byte> buffer = stackalloc byte[4];
            ReadExactly(stream, buffer);
            width = BitConverter.ToInt32(buffer);
            ReadExactly(stream, buffer);
            height = BitConverter.ToInt32(buffer);
            ReadExactly(stream, buffer);
            framerate = BitConverter.ToSingle(buffer);

            Debug.Log($"{width}x{height} @{framerate}");

            // must be run on main thread
            MainThreadDispatcher.Dispatch(() => m_sessionManager.BeginStream(width, height, framerate), MainThreadDispatcher.DispatchTarget.UPDATE);

            // wait 5 seconds to allow ffmpeg to start
            Thread.Sleep(TimeSpan.FromSeconds(5));

            // send back value greater than 0 back
            // send as byte - receiver is expecting char
            sw.Write((char)1);
            sw.Flush();
        }

        private void ExitStream()
        {
            // must be run on main thread
            MainThreadDispatcher.Dispatch(() => m_sessionManager.EndStream(), MainThreadDispatcher.DispatchTarget.UPDATE);
        }

        private void EnsureServer()
        {
            if (m_server == null) throw new InvalidOperationException("The server has not been started.");
        }

        private void ReadExactly(Stream stream, Span<byte> buffer)
        {
            int read = 0;
            while (read < buffer.Length)
            {
                read += stream.Read(buffer.Slice(read));
            }

            System.Diagnostics.Debug.Assert(read == buffer.Length);
        }
    }
}