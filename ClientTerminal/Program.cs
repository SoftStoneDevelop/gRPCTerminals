namespace ClientTerminal
{
    internal class Program
    {
        static void Main()
        {
            var provider = new ClientProvider("127.0.0.1", 6024, Guid.NewGuid().ToString());
            Console.WriteLine("Write 'Exit' for exit");
            while(true)
            {
                var line = Console.ReadLine();
                if(line == "Exit")
                    break;

                var command = new gRPCDefinition.CommandRequest();
                command.Guid = Guid.NewGuid().ToString();
                command.ProcessType = gRPCDefinition.ProcessType.Queue;

                provider.SendCommand(command);
            }
            provider.Dispose();
        }
    }
}