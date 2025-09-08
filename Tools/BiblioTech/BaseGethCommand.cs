using BiblioTech.Options;
using ArchivistContractsPlugin;
using GethPlugin;

namespace BiblioTech
{
    public abstract class BaseGethCommand : BaseCommand
    {
        protected override async Task Invoke(CommandContext context)
        {
            if (Program.GethLink == null)
            {
                await context.Followup("Blockchain operations are (temporarily) unavailable.");
                return;
            }

            var gethNode = Program.GethLink.Node;
            var contracts = Program.GethLink.Contracts;

            if (!contracts.IsDeployed())
            {
                await context.Followup("I'm sorry, the Archivist SmartContracts are not currently deployed.");
                return;
            }

            await Execute(context, gethNode, contracts);
        }

        protected abstract Task Execute(CommandContext context, IGethNode gethNode, IArchivistContracts contracts);
    }
}
