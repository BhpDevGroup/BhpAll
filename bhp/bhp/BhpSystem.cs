﻿using Akka.Actor;
using Bhp.Consensus;
using Bhp.Ledger;
using Bhp.Network.P2P;
using Bhp.Network.RPC;
using Bhp.Persistence;
using Bhp.Plugins;
using Bhp.Wallets;
using System;
using System.Net;

namespace Bhp
{
    public class BhpSystem : IDisposable
    {
        public ActorSystem ActorSystem { get; } = ActorSystem.Create(nameof(BhpSystem),
            $"akka {{ log-dead-letters = off }}" +
            $"blockchain-mailbox {{ mailbox-type: \"{typeof(BlockchainMailbox).AssemblyQualifiedName}\" }}" +
            $"task-manager-mailbox {{ mailbox-type: \"{typeof(TaskManagerMailbox).AssemblyQualifiedName}\" }}" +
            $"remote-node-mailbox {{ mailbox-type: \"{typeof(RemoteNodeMailbox).AssemblyQualifiedName}\" }}" +
            $"protocol-handler-mailbox {{ mailbox-type: \"{typeof(ProtocolHandlerMailbox).AssemblyQualifiedName}\" }}" +
            $"consensus-service-mailbox {{ mailbox-type: \"{typeof(ConsensusServiceMailbox).AssemblyQualifiedName}\" }}");
        public IActorRef Blockchain { get; }
        public IActorRef LocalNode { get; }
        internal IActorRef TaskManager { get; }
        public IActorRef Consensus { get; private set; }
        public RpcServer RpcServer { get; private set; }

        public BhpSystem(Store store)
        {
            this.Blockchain = ActorSystem.ActorOf(Ledger.Blockchain.Props(this, store));
            this.LocalNode = ActorSystem.ActorOf(Network.P2P.LocalNode.Props(this));
            this.TaskManager = ActorSystem.ActorOf(Network.P2P.TaskManager.Props(this));
            Plugin.LoadPlugins(this);
        }

        public void Dispose()
        {
            RpcServer?.Dispose();
            ActorSystem.Stop(LocalNode);
            ActorSystem.Dispose();
        }

        public void StartConsensus(Wallet wallet)
        {
            Consensus = ActorSystem.ActorOf(ConsensusService.Props(this, wallet));
            Consensus.Tell(new ConsensusService.Start());
        }

        public void StartNode(int port = 0, int ws_port = 0)
        {
            LocalNode.Tell(new Peer.Start
            {
                Port = port,
                WsPort = ws_port
            });
        }

        public void StartRpc(IPAddress bindAddress, int port, Wallet wallet = null, string sslCert = null, string password = null,
            string[] trustedAuthorities = null, Fixed8 maxGasInvoke = default(Fixed8))
        {
            RpcServer = new RpcServer(this, wallet, maxGasInvoke);
            RpcServer.Start(bindAddress, port, sslCert, password, trustedAuthorities);
        }
    }
}