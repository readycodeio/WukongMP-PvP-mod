using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.RPC;

namespace WukongMp.PvP;

public partial class PvpServerRpc(IRpcClient rpc) : ServerRpcClientBase(rpc);