using Microsoft.AspNetCore.Mvc;

namespace ChainStateAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MomentRequestsController : ControllerBase
    {
        [HttpPost]
        public ActiveContractsResponse GetActiveContracts([FromBody] MomentRequest request)
        {
            return new ActiveContractsResponse();
        }

        [HttpPost]
        public ContractsStateResponse GetContractsState([FromBody] ContractsStateRequest request)
        {
            return new ContractsStateResponse();
        }
    }

    public class MomentRequest
    {
        public DateTime Utc { get; set; }
    }

    public class ContractsStateRequest : MomentRequest
    {
        public string[] ContractIds { get; set; } = Array.Empty<string>();
    }

    public class ActiveContractsResponse
    {
        public string[] ContractIds { get; set; } = Array.Empty<string>();
    }

    public class ContractsStateResponse
    {
        public object[] ContractStates { get; set; } = Array.Empty<object>();
    }
}
