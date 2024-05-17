﻿using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Util;

namespace RabbitMQ.Client.ConsumerDispatching
{
#nullable enable
    internal abstract class ConsumerDispatcherBase
    {
        private static readonly FallbackConsumer fallbackConsumer = new FallbackConsumer();
        private readonly Dictionary<ReadOnlyMemory<byte>, (IBasicConsumer consumer, ConsumerTag consumerTag)> _consumers;

        public IBasicConsumer? DefaultConsumer { get; set; }

        protected ConsumerDispatcherBase()
        {
            _consumers = new Dictionary<ReadOnlyMemory<byte>, (IBasicConsumer, ConsumerTag)>(MemoryOfByteEqualityComparer.Instance);
        }

        protected void AddConsumer(IBasicConsumer consumer, ConsumerTag tag)
        {
            lock (_consumers)
            {
                byte[] tagBytes = Encoding.UTF8.GetBytes(tag);
                _consumers[tagBytes] = (consumer, tag);
            }
        }

        protected (IBasicConsumer consumer, ConsumerTag consumerTag) GetConsumerOrDefault(ReadOnlyMemory<byte> tag)
        {
            lock (_consumers)
            {
                if (_consumers.TryGetValue(tag, out (IBasicConsumer consumer, ConsumerTag consumerTag) consumerPair))
                {
                    return consumerPair;
                }

#if TODO
#if NET6_0_OR_GREATER
                string consumerTag = Encoding.UTF8.GetString(tag.Span);
#else
                string consumerTag;
                unsafe
                {
                    fixed (byte* bytes = tag.Span)
                    {
                        consumerTag = Encoding.UTF8.GetString(bytes, tag.Length);
                    }
                }
#endif
#endif
                return (GetDefaultOrFallbackConsumer(), new ConsumerTag(tag));
            }
        }

        public IBasicConsumer GetAndRemoveConsumer(ConsumerTag tag)
        {
            lock (_consumers)
            {
                ArrayPool<byte> pool = ArrayPool<byte>.Shared;
                byte[]? buf = null;
                try
                {
                    string tagStr = (string)tag;
                    buf = pool.Rent(tag.ByteCount);
#if NET6_0_OR_GREATER
                    int count = Encoding.UTF8.GetBytes(tagStr, buf);
#else
                    int count = Encoding.UTF8.GetBytes(tagStr, 0, tagStr.Length, buf, 0);
#endif
                    Memory<byte> memory = buf.AsMemory(0, count);
                    IBasicConsumer result = _consumers.Remove(memory,
                        out (IBasicConsumer consumer, ConsumerTag consumerTag) consumerPair) ?
                            consumerPair.consumer : GetDefaultOrFallbackConsumer();
                    return result;
                }
                finally
                {
                    if (buf != null)
                    {
                        pool.Return(buf);
                    }
                }
            }
        }

        public void Shutdown(ShutdownEventArgs reason)
        {
            DoShutdownConsumers(reason);
            InternalShutdown();
        }

        public Task ShutdownAsync(ShutdownEventArgs reason)
        {
            DoShutdownConsumers(reason);
            return InternalShutdownAsync();
        }

        private void DoShutdownConsumers(ShutdownEventArgs reason)
        {
            lock (_consumers)
            {
                foreach (KeyValuePair<ReadOnlyMemory<byte>, (IBasicConsumer consumer, ConsumerTag consumerTag)> pair in _consumers)
                {
                    ShutdownConsumer(pair.Value.consumer, reason);
                }
                _consumers.Clear();
            }
        }

        protected abstract void ShutdownConsumer(IBasicConsumer consumer, ShutdownEventArgs reason);

        protected abstract void InternalShutdown();

        protected abstract Task InternalShutdownAsync();

        // Do not inline as it's not the default case on a hot path
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IBasicConsumer GetDefaultOrFallbackConsumer()
        {
            return DefaultConsumer ?? fallbackConsumer;
        }
    }
}
