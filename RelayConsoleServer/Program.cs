using PQRelay;
using System.Text;

RelayServer server = new RelayServer();
string address = server.StartRelaying();

Console.WriteLine("Succesfully started server on: " + address);

//PrintStatus();
Console.WriteLine();
PrintCommands();

while (true)
{
    string input = Console.ReadLine();
    if (input == null) input = "";

    if (input == "help") PrintCommands();
    else if (input == "stop") return;
    else if (input == "status")
    {
        PrintStatus();
    }
    else if (input == "clear")
    {
        Console.Clear();
    }
}

void PrintStatus()
{
    (int clients, int[] pending, int active) status = server.Status();
    Console.WriteLine("Server status:");
    Console.WriteLine(" Server is " + (server.IsRunning() ? "running" : "not running"));
    if (server.IsRunning())
    {
        Console.WriteLine(" Relaying on: " + address);
        Console.WriteLine(" Uptime: " + server.Uptime());
        Console.WriteLine(" Clients connected: " + status.clients);
        Console.WriteLine(" Active relays: " + status.active);
        Console.WriteLine(" Pending connections: ");
        foreach (var pending in status.pending.OrderBy(k => k))
        {
            Console.WriteLine("  " + pending);
        }
    }

    Console.WriteLine();
}

void PrintCommands()
{
    Console.WriteLine("Valid commands:");
    Console.WriteLine(" help    - Shows all commands");
    Console.WriteLine(" stop    - Closes the server");
    Console.WriteLine(" status  - Shows server status");
    Console.WriteLine(" clear   - Clears the console");

    Console.WriteLine();
}