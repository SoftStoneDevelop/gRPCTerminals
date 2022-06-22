using gRPCDefinition;
using System.Threading.Channels;

namespace gRPCServer.Interfaces
{
    public interface ICommandProcessor
    {
        /// <summary>
        /// Command will be execute after current processed command.
        /// </summary>
        /// <param name="guidTerminal">Guid the requesting terminal</param>
        /// <param name="command"></param>
        void AddImmidiateCommand(string guidTerminal, CommandRequest command);

        /// <summary>
        /// Put a command at the end of the queue
        /// </summary>
        /// <param name="guid">Guid the requesting terminal</param>
        /// <param name="command"></param>
        void EnqueueCommand(string guid, CommandRequest command);

        /// <summary>
        /// Add new terminal
        /// </summary>
        /// <param name="guid">terminal guid</param>
        /// <param name="reader">answer reader</param>
        /// <returns>true if terminal with <paramref name="guid"/> does not already exist in the processor</returns>
        bool AddClientTerminal(string guid, out ChannelReader<CommandResponce> reader);

        /// <summary>
        /// Destroy terminal chanel
        /// </summary>
        /// <param name="guid">terminal guid</param>
        /// <returns>true if terminal with <paramref name="guid"/> exist</returns>
        bool DestroyClientTerminal(string guid);
    }
}