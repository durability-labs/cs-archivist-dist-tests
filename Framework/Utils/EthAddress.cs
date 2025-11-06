namespace Utils
{
    public interface IHasEthAddress
    {
        EthAddress EthAddress { get; }
    }

    [Serializable]
    public class EthAddress : IComparable<EthAddress>
    {
        public EthAddress(string address)
        {
            if (string.IsNullOrEmpty(address)) throw new Exception("Invalid EthAddress");
            Address = address.ToLowerInvariant();
        }

        public string Address { get; }

        public int CompareTo(EthAddress? other)
        {
            return Address.CompareTo(other!.Address);
        }

        public override bool Equals(object? obj)
        {
            return obj is EthAddress token && Address == token.Address;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Address);
        }

        public override string ToString()
        {
            return Address;
        }

        public static bool operator ==(EthAddress? a, EthAddress? b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (ReferenceEquals(a, null)) return false;
            if (ReferenceEquals(b, null)) return false;
            return a.Address == b.Address;
        }

        public static bool operator !=(EthAddress? a, EthAddress? b)
        {
            return !(a == b);
        }
    }

    public class ContractAddress : EthAddress
    {
        public ContractAddress(string address) : base(address)
        {
        }
    }

    public static class EthAddressExtensions
    {
        public static string AsStr(this EthAddress? addressMaybe)
        {
            if (addressMaybe == null) return "-";
            return addressMaybe.ToString();
        }
    }
}
