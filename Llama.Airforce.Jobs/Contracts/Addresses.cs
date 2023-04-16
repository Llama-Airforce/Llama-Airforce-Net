using Llama.Airforce.SeedWork.Types;

namespace Llama.Airforce.Jobs.Contracts;

public static class Addresses
{
    public static class ERC20
    {
        public static Address WETH = Address.Of("0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2");
        public static Address T = Address.Of("0xcdf7028ceab81fa0c6971208e83fa7872994bee5");
        public static Address eCFX = Address.Of("0xa1f82e14bc09a1b42710df1a8a999b62f294e592");
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

        public static Address FeeDistributor = Address.Of("0xA464e6DCda8AC41e03616F95f4BC98a13b8922Dc");
        public static Address StableSwapProxy = Address.Of("0xeCb456EA5365865EbAb8a2661B0c503410e9B347");
    }

    public static class Convex
    {
        public static Address Token = Address.Of("0x4e3fbd56cd56c3e72c1403e103b45db9da5b9d2b");
        public static Address Locked = Address.Of("0xD18140b4B819b895A3dba5442F959fA44994AF50");
        public static Address Locked2 = Address.Of("0x72a19342e8f1838460ebfccef09f6585e32db86e");
        public static Address Staked = Address.Of("0xCF50b810E57Ac33B91dCF525C6ddd9881B139332");

        public static Address VoterProxy = Address.Of("0x989AEb4d175e16225E39E87d0D97A3360524AD80");
    }

    public static class CvxCrv
    {
        public static Address Token = Address.Of("0x62B9c7356A2Dc64a1969e19C23e4f579F9810Aa7");
        public static Address Staked = Address.Of("0x3Fe65692bfCD0e6CF84cB1E7d24108E434A7587e");
    }

    public static class CurveV2LP
    {
        public static Address TETH = Address.Of("0x752eBeb79963cf0732E9c0fec72a49FD1DEfAEAC");
        public static Address eCFXETH = Address.Of("0x5ac4fcee123dcadfae22bc814c4cc72b96c93f38");
    }

    public static class HiddenHand
    {
        public static Address AuraBribeVault = Address.Of("0x9ddb2da7dd76612e0df237b89af2cf4413733212");
    }

    public static class Balancer
    {
        public static Address Token = Address.Of("0xba100000625a3754423978a60c9317c58a424e3D");
        public static Address TokenAdmin = Address.Of("0xf302f9F50958c5593770FDf4d4812309fF77414f");
        public static Address BBAUSDToken = Address.Of("0x7b50775383d3d6f0215a8f290f2c9e2eebbeceb2");

        public static Address GaugeController = Address.Of("0xC128468b7Ce63eA702C1f104D55A2566b13D3ABD");
        public static Address VotingEscrow = Address.Of("0xC128a9954e6c874eA3d62ce62B468bA073093F25");

        public static Address Vault = Address.Of("0xba12222222228d8ba445958a75a0704d566bf2c8");
        public static Address BPT = Address.Of("0x5c6Ee304399DBdB9C8Ef030aB642B10820DB8F56");
    }

    public static class AuraBal
    {
        public static Address Token = Address.Of("0x616e8bfa43f920657b3497dbf40d6b1a02d4608d");
    }

    public static class Aura
    {
        public static Address Token = Address.Of("0xC0c293ce456fF0ED870ADd98a0828Dd4d2903DBF");
        public static Address Locked = Address.Of("0x3Fa73f1E5d8A792C80F426fc8F84FBF7Ce9bBCAC");

        public static Address BalStaked = Address.Of("0x00A7BA8Ae7bca0B10A32Ea1f8e2a1Da980c6CAd2"); // auraBAL
        public static Address BBAUSDStaked = Address.Of("0xfd176ba656b91f0ce8c59ad5c3245bebb99cd69a");

        public static Address VoterProxy = Address.Of("0xaF52695E1bB01A16D33D7194C28C42b10e0Dbec2");
    }
}