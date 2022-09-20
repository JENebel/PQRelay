using SecureTCP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PQRelay
{
    public class RelayClient
    {
        SecureTcpClient tcpClient;
        public bool Established { get; private set; } = false;

        public event EventHandler<string> ConnectionEstablished;
        public event EventHandler<MessageReceivedEventArgs> DataReceived;
        public event EventHandler<ClientDisconnectedEventArgs> Disconnected;
        public bool Connected { get { return tcpClient.Connected; } }

        public RelayClient()
        {
            tcpClient = new SecureTcpClient();

            tcpClient.ClientDisconnected += TcpClient_ClientDisconnected;
            tcpClient.MessageReceived += TcpClient_MessageReceived;
        }

        private void TcpClient_MessageReceived(object? sender, MessageReceivedEventArgs e)
        {
            if (!Established && e.Data.SequenceEqual(new byte[] { 1 })) 
            {
                Established = true;
                ConnectionEstablished(this, "Succesfully established link"); 
                return;
            }
            else
                DataReceived(this, e);
        }

        public void Disconnect()
        {
            tcpClient.Disconnect();
        }

        private void TcpClient_ClientDisconnected(object? sender, ClientDisconnectedEventArgs e)
        {
            if(Disconnected != null) Disconnected(this, e);
        }

        public void RelayData(byte[] data)
        {
            tcpClient.Send(data);
        }

        public async Task<int> RegisterOnServer(string serverIPPort)
        {
            try
            {
                (string ip, ushort port) serverAddress = IPPortStringSplitter(serverIPPort);
                await tcpClient.Connect(serverAddress.ip, serverAddress.port);
                if (!tcpClient.Connected) throw new Exception("Failed to connect");
                var resp = await tcpClient.SendAndWait(new byte[] { 1 });
                return BitConverter.ToInt32(resp);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task<bool> ConnectToExistingAsync(string serverIPPort, int sessionID)
        {
            try
            {
                (string ip, ushort port) serverAddress = IPPortStringSplitter(serverIPPort);
                await tcpClient.Connect(serverAddress.ip, serverAddress.port);
                tcpClient.Send(BitConverter.GetBytes(sessionID));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private (string ip, ushort port) IPPortStringSplitter(string IPPort)
        {
            string ip = IPPort.Split(":")[0];
            ushort port = ushort.Parse(IPPort.Split(":")[1]);

            return (ip, port);
        }
    }
}
