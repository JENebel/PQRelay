using PQRelay;
using SecureTCP;
using System.Text;

//string address = "80.162.42.214:9488";
string address = "192.168.15.58:9488";

/*
RelayServer server = new RelayServer();
Console.WriteLine("Starting server");
string address = server.StartRelaying();
Console.WriteLine("Server running on " + address);
*/
Console.WriteLine("Connecting to: " + address);
RelayClient client1 = new();
int id = await client1.RegisterOnServer(address);
if (id == 0) Error("Failed to register");

Console.WriteLine("Session ID is: " + id);


RelayClient client2 = new();

client1.ConnectionEstablished += Client_ConnectionEstablished;
client2.ConnectionEstablished += Client2_ConnectionEstablished;
client2.DataReceived += Client2_DataReceived;
client1.DataReceived += Client1_DataReceived;
client1.Disconnected += Client1_Disconnected;
client2.Disconnected += Client2_Disconnected;

bool res = await client2.ConnectToExistingAsync(address, id);
if (!res) Error("Could not connect to existing");

void Client2_Disconnected(object? sender, ClientDisconnectedEventArgs e)
{
    Console.WriteLine("C2 Disconnected!");
}

void Client1_Disconnected(object? sender, SecureTCP.ClientDisconnectedEventArgs e)
{
    Console.WriteLine("C1 Disconnected!");
}

void Client1_DataReceived(object? sender, SecureTCP.MessageReceivedEventArgs e)
{
    Console.WriteLine("C1: Message recieved: " + Encoding.ASCII.GetString(e.Data));
}

void Client2_DataReceived(object? sender, SecureTCP.MessageReceivedEventArgs e)
{
    Console.WriteLine("C2: Message recieved: " + Encoding.ASCII.GetString(e.Data));
}

void Client_ConnectionEstablished(object? sender, string e)
{
    Console.WriteLine("Connected! " + e);
}

void Client2_ConnectionEstablished(object? sender, string e)
{
    Console.WriteLine("Connected! " + e);
    
    string msg = "TestMsg";
    client2.RelayData(Encoding.ASCII.GetBytes(msg));
    Console.WriteLine("Message sent: " + msg);
}

Console.ReadLine();

void Error(string err)
{
    Console.WriteLine(err);
    Console.ReadLine();
}