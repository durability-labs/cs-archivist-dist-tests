using Microsoft.AspNetCore.Mvc;

namespace ChainStateAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DeploymentController : ControllerBase
    {
        [HttpGet]
        public DeploymentInfo GetDeploymentInfo()
        {
            return new DeploymentInfo();
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
