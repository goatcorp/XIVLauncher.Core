using System.Threading.Tasks;
using DiscordRPCBridge_Wine;

namespace XIVLauncher.Common.Unix;

public class UnixDiscordRpcRunner(int? port = null)
{
    private readonly RPCBridgeServer rpcBridge = new RPCBridgeServer();
    private readonly int? port = port;

    public void StartRpcBridge()
    {
        if (this.port.HasValue)
        {
            this.rpcBridge.Start(this.port.Value);
            return;
        }
        this.rpcBridge.Start();
    }
    public async Task StopRpcBridge() => 
        await this.rpcBridge.StopAsync().ConfigureAwait(false);
}
