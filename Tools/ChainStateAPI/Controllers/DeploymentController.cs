using ArchivistContractsPlugin;
using ArchivistContractsPlugin.Marketplace;
using ChainStateAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChainStateAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DeploymentController : ControllerBase
    {
        private readonly DeploymentInfo info;

        public DeploymentController(IDeploymentService deploymentService)
        {
            info = Map(deploymentService);
        }

        [HttpGet]
        public DeploymentInfo GetDeploymentInfo()
        {
            return info;
        }

        private DeploymentInfo Map(IDeploymentService deploymentService)
        {
            return new DeploymentInfo
            {
                RpcEndpoint = deploymentService.RpcEndpoint,
                MarketplaceContractAddress = deploymentService.MarketplaceContractAddress,
                DeploymentConfig = Map(deploymentService.Contracts.Deployment)
            };
        }

        private DeploymentConfigInfo Map(ArchivistContractsDeployment deployment)
        {
            return new DeploymentConfigInfo
            {
                CollateralConfig = Map(deployment.Config.Collateral),
                ProofConfig = Map(deployment.Config.Proofs),
                RequestDurationLimit = deployment.Config.RequestDurationLimit,
                SlotReservationsConfig = Map(deployment.Config.Reservations)
            };
        }

        private SlotReservationsConfigInfo Map(SlotReservationsConfig reservations)
        {
            return new SlotReservationsConfigInfo
            {
                MaxReservations = reservations.MaxReservations,
            };
        }

        private ProofConfigInfo Map(ProofConfig proofs)
        {
            return new ProofConfigInfo
            {
                Downtime = proofs.Downtime,
                DowntimeProduct = proofs.DowntimeProduct,
                Period = proofs.Period,
                Timeout = proofs.Timeout,
                ZkeyHash = proofs.ZkeyHash
            };
        }

        private CollateralConfigInfo Map(CollateralConfig collateral)
        {
            return new CollateralConfigInfo
            {
                MaxNumberOfSlashes = collateral.MaxNumberOfSlashes,
                RepairRewardPercentage = collateral.RepairRewardPercentage,
                SlashPercentage = collateral.SlashPercentage,
                ValidatorRewardPercentage = collateral.ValidatorRewardPercentage
            };
        }
    }

    public class DeploymentInfo
    {
        public string RpcEndpoint { get; set; } = string.Empty;
        public string MarketplaceContractAddress { get; set; } = string.Empty;
        public DeploymentConfigInfo DeploymentConfig { get; set; } = new();
    }

    public class DeploymentConfigInfo
    {
        public CollateralConfigInfo CollateralConfig { get; set; } = new();
        public ProofConfigInfo ProofConfig { get; set; } = new();
        public SlotReservationsConfigInfo SlotReservationsConfig { get; set; } = new();
        public ulong RequestDurationLimit { get; set; }
    }

    public class CollateralConfigInfo
    {
        public byte RepairRewardPercentage { get; set; }
        public byte MaxNumberOfSlashes { get; set; }
        public byte SlashPercentage { get; set; }
        public byte ValidatorRewardPercentage { get; set; }
    }

    public class ProofConfigInfo
    {
        public ulong Period { get; set; }
        public ulong Timeout { get; set; }
        public byte Downtime { get; set; }
        public byte DowntimeProduct { get; set; }
        public string ZkeyHash { get; set; } = string.Empty;
    }

    public class SlotReservationsConfigInfo
    {
        public byte MaxReservations { get; set; }
    }
}
