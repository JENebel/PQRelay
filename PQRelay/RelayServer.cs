using SecureTCP;
using System.Net;
using System.Net.Sockets;

namespace PQRelay
{
    public class RelayServer
    {
        SecureTcpServer tcpServer;

        Dictionary<int, string> pendingConnections;
        Dictionary<string, string> activeConnections;
        private Random random;
        DateTime startTime;
        bool debug = true;

        public RelayServer()
        {
            tcpServer = new(GetIP(), 9488);

            pendingConnections = new Dictionary<int, string>();
            activeConnections = new Dictionary<string, string>();

            tcpServer.ClientDisconnected += TcpServer_ClientDisconnected;
            tcpServer.MessageReceived += Server_MessageReceived;
            tcpServer.ClientConnected += TcpServer_ClientConnected;

            random = new Random();
        }

        private void TcpServer_ClientConnected(object? sender, ClientConnectedEventArgs e)
        {
            Debug(e.IpPort + " connected to server");
        }

        public bool IsRunning()
        {
            return tcpServer.Running;
        }

        public (int clients, int[] pending, int active) Status()
        {
            return (
                tcpServer.Clients.Length,
                pendingConnections.Keys.ToArray(),
                activeConnections.Count / 2
                );
        }

        public string StartRelaying(bool portForward = false)
        {
            tcpServer.Start();
            tcpServer.Respond = Response;

            startTime = DateTime.Now;

            Task.Run(() =>
            {
                while (true)
                {
                    Thread.Sleep(3600000);
                    Console.WriteLine(DateTime.Now.ToShortDateString() + " - " + DateTime.Now.ToShortTimeString() + ": Up time: " + Uptime());
                }
            });

            Debug("Started succesfully");
            return tcpServer.IpPort;
        }

        public TimeSpan Uptime()
        {

            TimeSpan time = DateTime.Now.Subtract(startTime);

            return new TimeSpan(time.Days, time.Hours, time.Minutes, time.Seconds);
        }

        private void Server_MessageReceived(object? sender, MessageReceivedEventArgs e)
        {
            Debug("Message received from " + e.IpPort);
            //Relay
            if (activeConnections.ContainsKey(e.IpPort))
            {
                Debug("Relaying message from " + e.IpPort + " to " + activeConnections[e.IpPort]);
                tcpServer.Send(e.Data, activeConnections[e.IpPort]);
            }
            else //Activate connection
            {
                int id = BitConverter.ToInt32(e.Data);
                if (!pendingConnections.ContainsKey(id))
                    tcpServer.Send(new byte[] { 0 }, e.IpPort);

                Debug("Request for " + id + " received from " + e.IpPort);

                //Activate relay
                activeConnections.Add(e.IpPort, pendingConnections[id]);
                activeConnections.Add(pendingConnections[id], e.IpPort);

                //Send confirmation
                tcpServer.Send(new byte[] { 1 }, e.IpPort);
                tcpServer.Send(new byte[] { 1 }, pendingConnections[id]);

                Debug("Relay established between " + e.IpPort + " and " + pendingConnections[id]);

                pendingConnections.Remove(id);
            }
        }

        private void TcpServer_ClientDisconnected(object? sender, ClientDisconnectedEventArgs e)
        {
            Debug(e.IpPort + " disconnected, reason: " + e.Reason);
            foreach (var item in pendingConnections)
            {
                if (item.Value == e.IpPort)
                {
                    pendingConnections.Remove(item.Key);
                    break;
                }
            }
            if (activeConnections.ContainsKey(e.IpPort))
            {
                string peer = activeConnections[e.IpPort];
                activeConnections.Remove(peer);
                activeConnections.Remove(e.IpPort);
                tcpServer.Disconnect(peer);
            }
        }

        private byte[] Response(byte[] rawReq, string ipPort)
        {
            Debug("Request received from " + ipPort);

            if (rawReq[0] == 1)
            {
                //Generate code
                int code = -1;
                do
                {
                    code = random.Next(1000, 10000);
                } while (pendingConnections.ContainsKey(code));

                pendingConnections.Add(code, ipPort);
                Debug("Setup code: " + code + " for " + ipPort);
                return BitConverter.GetBytes(code);
            }
            return new byte[] { 0 };
        }

        private string GetIP()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            string res = "";
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    res = ip.ToString();
                }
            }
            if (res != "") return res;
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        private void Debug(string db)
        {
            if (debug)
                Console.WriteLine("SERVER DEBUG: " + db);
        }
    }

}