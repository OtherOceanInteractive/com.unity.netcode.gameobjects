using System;

namespace Unity.Netcode
{
    public class ExposedMetricHook
    {
        public Action<int> BytesSent = (bytes) => { };
        public Action<int> BytesReceived = (bytes) => { };

        public ExposedMetricHook()
        {
            m_InternalMetricHook = new InternalMetricHook(this);
        }

        public void Hook(Action<int> bytesSent, Action<int> bytesReceived)
        {
            BytesSent += bytesSent;
            BytesReceived += bytesReceived;
            NetworkManager.OnSingletonReady += () =>
            {
                NetworkManager.Singleton.MessagingSystem.Hook(m_InternalMetricHook);
            };
        }

        public void UnHookAll()
        {
            BytesSent = (bytes) => { };
            BytesReceived = (bytes) => { };
            NetworkManager.Singleton.MessagingSystem.Unhook(m_InternalMetricHook);
        }

        private readonly InternalMetricHook m_InternalMetricHook;
    }

    internal class InternalMetricHook : INetworkHooks
    {
        private ExposedMetricHook m_ExposedMetricHook;
        public InternalMetricHook(ExposedMetricHook exposedMetricHook)
        {
            m_ExposedMetricHook = exposedMetricHook;
        }

        public void OnBeforeSendMessage<T>(ulong clientId, ref T message, NetworkDelivery delivery)
            where T : INetworkMessage
        {
        }

        public void OnAfterSendMessage<T>(ulong clientId, ref T message, NetworkDelivery delivery, int messageSizeBytes)
            where T : INetworkMessage
        {
        }

        public void OnBeforeReceiveMessage(ulong senderId, Type messageType, int messageSizeBytes)
        {
        }

        public void OnAfterReceiveMessage(ulong senderId, Type messageType, int messageSizeBytes)
        {
        }

        public void OnBeforeSendBatch(ulong clientId, int messageCount, int batchSizeInBytes, NetworkDelivery delivery)
        {
        }

        public void OnAfterSendBatch(ulong clientId, int messageCount, int batchSizeInBytes, NetworkDelivery delivery)
        {
            m_ExposedMetricHook.BytesSent.Invoke(batchSizeInBytes);
        }

        public void OnBeforeReceiveBatch(ulong senderId, int messageCount, int batchSizeInBytes)
        {
            m_ExposedMetricHook.BytesReceived.Invoke(batchSizeInBytes);
        }

        public void OnAfterReceiveBatch(ulong senderId, int messageCount, int batchSizeInBytes)
        {
        }

        public bool OnVerifyCanSend(ulong destinationId, Type messageType, NetworkDelivery delivery)
        {
            return true;
        }

        public bool OnVerifyCanReceive(ulong senderId, Type messageType, FastBufferReader messageContent,
            ref NetworkContext context)
        {
            return true;
        }

        public void OnBeforeHandleMessage<T>(ref T message, ref NetworkContext context) where T : INetworkMessage
        {
            // TODO: Per-message metrics recording moved here
        }

        public void OnAfterHandleMessage<T>(ref T message, ref NetworkContext context) where T : INetworkMessage
        {
            // TODO: Per-message metrics recording moved here
        }
    }
}
