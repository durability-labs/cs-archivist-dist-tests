using Nethereum.Hex.HexConvertors.Extensions;
using Utils;

namespace TraceContract
{
    public class Input
    {
        public string PurchaseId
        {
            get
            {
                return EnvVar.GetOrThrow("PURCHASE_ID");
            }
        }

        public byte[] RequestId
        {
            get
            {
                var r = PurchaseId.HexToByteArray();
                if (r == null || r.Length != 32) throw new ArgumentException(nameof(PurchaseId));
                return r;
            }
        }
    }
}
