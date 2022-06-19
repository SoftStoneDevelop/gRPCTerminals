using gRPCDefinition;
using System.Threading.Channels;

namespace gRPCServer.Interfaces
{
    public interface ICommandProcessor
    {
        void EnqueueCommand(CommandRequest command);
        bool AddClientTerminal(Guid guid, out ChannelReader<CommandResponce> reader);
        bool DestroyClientTerminal(Guid guid);
    }
}