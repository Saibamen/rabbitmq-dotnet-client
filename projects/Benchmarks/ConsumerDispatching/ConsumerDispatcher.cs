using System;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using RabbitMQ.Client;
using RabbitMQ.Client.ConsumerDispatching;

namespace RabbitMQ.Benchmarks
{
    [Config(typeof(Config))]
    [BenchmarkCategory("ConsumerDispatcher")]
    public class ConsumerDispatcherBase
    {
        protected static readonly ManualResetEventSlim _autoResetEvent = new ManualResetEventSlim(false);

        private protected IConsumerDispatcher _dispatcher;
        private protected readonly AsyncBasicConsumerFake _consumer = new AsyncBasicConsumerFake(_autoResetEvent);

        protected static readonly ConsumerTag _consumerTag = new ConsumerTag("ConsumerTag");
        protected static readonly ExchangeName _exchange = new ExchangeName("Exchange");
        protected static readonly RoutingKey _routingKey = new RoutingKey("RoutingKey");

        protected readonly ulong _deliveryTag = 500UL;
        protected readonly ReadOnlyBasicProperties _properties = new ReadOnlyBasicProperties();
        protected readonly byte[] _method = new byte[512];
        protected readonly byte[] _body = new byte[512];

        public ConsumerDispatcherBase()
        {
            var r = new Random();
            r.NextBytes(_body);
        }
    }

    public class BasicDeliverConsumerDispatching : ConsumerDispatcherBase
    {
        [Params(1, 30)]
        public int Count { get; set; }

        [Params(1, 2)]
        public int Concurrency { get; set; }

        [GlobalSetup(Target = nameof(AsyncConsumerDispatcher))]
        public async Task SetUpAsyncConsumer()
        {
            _consumer.Count = Count;
            _dispatcher = new AsyncConsumerDispatcher(null, Concurrency);
            await _dispatcher.HandleBasicConsumeOkAsync(_consumer, _consumerTag, CancellationToken.None);
        }

        [Benchmark]
        public async Task AsyncConsumerDispatcher()
        {
            var m = new MethodBasicDeliver();
            m.SetUp();
            using (RentedMemory method = new RentedMemory(m.Buffer.ToArray()))
            using (RentedMemory body = new RentedMemory(_body))
            {
                for (int i = 0; i < Count; i++)
                {
                    await _dispatcher.HandleBasicDeliverAsync(_deliveryTag,
                        false, _properties, method, body, CancellationToken.None);
                }
                _autoResetEvent.Wait();
                _autoResetEvent.Reset();
            }
        }

        [GlobalSetup(Target = nameof(ConsumerDispatcher))]
        public async Task SetUpConsumer()
        {
            _consumer.Count = Count;
            _dispatcher = new ConsumerDispatcher(null, Concurrency);
            await _dispatcher.HandleBasicConsumeOkAsync(_consumer, _consumerTag, CancellationToken.None);
        }

        [Benchmark]
        public async Task ConsumerDispatcher()
        {
            var m = new MethodBasicDeliver();
            m.SetUp();
            using (RentedMemory method = new RentedMemory(m.Buffer.ToArray()))
            using (RentedMemory body = new RentedMemory(_body))
            {
                for (int i = 0; i < Count; i++)
                {
                    await _dispatcher.HandleBasicDeliverAsync(_deliveryTag,
                        false, _properties, method, body, CancellationToken.None);
                }
                _autoResetEvent.Wait();
                _autoResetEvent.Reset();
            }
        }
    }
}
