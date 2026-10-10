using Llama.Airforce.SeedWork.Types;

namespace Llama.Airforce.Jobs.Contracts;

public static class Addresses
{
    public static class ERC20
    {
        public static Address WETH = Address.Of("0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2");
        public static Address T = Address.Of("0xcdf7028ceab81fa0c6971208e83fa7872994bee5");
        public static Address eCFX = Address.Of("0xa1f82e14bc09a1b42710df1a8a999b62f294e592");
        public static Address sdFXS = Address.Of("0x402f878bdd1f5c66fdaf0fababcf74741b68ac36");
        public static Address FXS = Address.Of("0x3432b6a60d23ca0dfca7761b7ab56459d9c964d0");
    }

    public static class Curve
    {
        public static Address Token = Address.Of("0xD533a949740bb3306d119CC777fa900bA034cd52");
        public static Address Staked = Address.Of("0x3Fe65692bfCD0e6CF84cB1E7d24108E434A7587e"); // cvxCRV

        public static Address ThreePoolStaked = Address.Of("0x7091dbb7fcbA54569eF1387Ac89Eb2a5C9F6d2EA");

        public static Address CurveSwap = Address.Of("0xbEbc44782C7dB0a1A60Cb6fe97d0b483032FF1C7");

        /// <summary>
        /// This is the veCRV token.
        /// </summary>
        public static Address VotingEscrow = Address.Of("0x5f3b5DfEb7B28CDbD7FAba78963EE202a494e2A2");

        public static Address GaugeController = Address.Of("0x2F50D538606Fa9EDD2B11E2446BEb18C9D5846bB");
    }

    public static class Convex
    {
        public static Address Token = Address.Of("0x4e3fbd56cd56c3e72c1403e103b45db9da5b9d2b");
        public static Address Locked = Address.Of("0xD18140b4B819b895A3dba5442F959fA44994AF50");
        public static Address Locked2 = Address.Of("0x72a19342e8f1838460ebfccef09f6585e32db86e");

        public static Address VoterProxyCurve = Address.Of("0x989AEb4d175e16225E39E87d0D97A3360524AD80");
        public static Address VoterProxyFxn = Address.Of("0xd11a4Ee017cA0BECA8FA45fF2abFe9C6267b7881");

        public static Address Core = Address.Of("0xCC07e8BA6bc8aeb18C4AE110C3Da9c7Dce4A3e74");
        public static Address VotingRegistry = Address.Of("0x6C3a56ea7c2DA4ee2876DbDa437173B28f001c34");
        public static Address GaugeDelegation = Address.Of("0xb8270eef1319173dE9f5033FED442F638ff1607d");
        public static Address GaugeVoteHelper = Address.Of("0x76C484F67898EA978aa874dc7B32e648380FB9b1");
        public static Address CurveGaugeVoting = Address.Of("0x64D9B5AC386B70af9EDCD20A58cE9262D2EAC278");
        public static Address CurveGaugeRegistry = Address.Of("0x96b24E0534B0cA31D8523D4be4904747Fd579D95");
        public static Address FxGaugeVoting = Address.Of("0xDcEa673B021f1f431E7D0Ec70a63bF8DcB6d44E6");
        public static Address FxGaugeRegistry = Address.Of("0xC2F99261d84D1665Df7e50F46813497A737AfB20");
    }

    public static class CvxCrv
    {
        public static Address Token = Address.Of("0x62B9c7356A2Dc64a1969e19C23e4f579F9810Aa7");
    }

    public static class CurveV1LP
    {
        public static Address FXSsdFXS = Address.Of("0x8c524635d52bd7b1bd55e062303177a7d916c046");
    }

    public static class CurveV2LP
    {
        public static Address TETH = Address.Of("0x752eBeb79963cf0732E9c0fec72a49FD1DEfAEAC");
        public static Address eCFXETH = Address.Of("0x5ac4fcee123dcadfae22bc814c4cc72b96c93f38");
    }

    public static class UniV3Pools
    {
        public static Address TXJPWETH = Address.Of("0xa9166690c35d900a57d2ec132c58291bc0678944");
    }

    public static class Fxn
    {
        public static Address Token = Address.Of("0x365AccFCa291e7D3914637ABf1F7635dB165Bb09");
        public static Address Locker = Address.Of("0xec6b8a3f3605b083f7044c0f31f2cac0caf1d469");
    }
}
