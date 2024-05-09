// This source code is dual-licensed under the Apache License, version
// 2.0, and the Mozilla Public License, version 2.0.
//
// The APL v2.0:
//
//---------------------------------------------------------------------------
//   Copyright (c) 2007-2020 VMware, Inc.
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       https://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
//---------------------------------------------------------------------------
//
// The MPL v2.0:
//
//---------------------------------------------------------------------------
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
//  Copyright (c) 2007-2020 VMware, Inc.  All rights reserved.
//---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using Xunit;

namespace Test.Unit
{
    public class TestEndpointResolver : IEndpointResolver
    {
        private readonly IEnumerable<AmqpTcpEndpoint> _endpoints;
        public TestEndpointResolver(IEnumerable<AmqpTcpEndpoint> endpoints)
        {
            _endpoints = endpoints;
        }

        public IEnumerable<AmqpTcpEndpoint> All()
        {
            return _endpoints;
        }
    }

    class TestEndpointException : Exception
    {
        public TestEndpointException(string message) : base(message)
        {
        }
    }

    public class TestIEndpointResolverExtensions
    {
        [Fact]
        public async Task SelectOneShouldReturnDefaultWhenThereAreNoEndpoints()
        {
            var ep = new TestEndpointResolver(new List<AmqpTcpEndpoint>());

            Task<AmqpTcpEndpoint> selector(AmqpTcpEndpoint ep0, CancellationToken ct)
            {
                return Task.FromResult<AmqpTcpEndpoint>(null);
            }

            Assert.Null(await ep.SelectOneAsync<AmqpTcpEndpoint>(selector, CancellationToken.None));
        }

        [Fact]
        public async Task SelectOneShouldRaiseThrownExceptionWhenThereAreOnlyInaccessibleEndpoints()
        {
            var ep = new TestEndpointResolver(new List<AmqpTcpEndpoint> { new AmqpTcpEndpoint() });

            Task<AmqpTcpEndpoint> selector(AmqpTcpEndpoint ep0, CancellationToken ct)
            {
                return Task.FromException<AmqpTcpEndpoint>(new TestEndpointException("bananas"));
            }

            Task<AmqpTcpEndpoint> testCode()
            {
                return ep.SelectOneAsync(selector, CancellationToken.None);
            }

            AggregateException ex = await Assert.ThrowsAsync<AggregateException>((Func<Task<AmqpTcpEndpoint>>)testCode);
            Assert.Single(ex.InnerExceptions);
            Assert.All(ex.InnerExceptions, e => Assert.IsType<TestEndpointException>(e));
        }

        [Fact]
        public async Task SelectOneShouldReturnFoundEndpoint()
        {
            var ep = new TestEndpointResolver(new List<AmqpTcpEndpoint> { new AmqpTcpEndpoint() });

            Task<AmqpTcpEndpoint> selector(AmqpTcpEndpoint ep0, CancellationToken ct)
            {
                return Task.FromResult<AmqpTcpEndpoint>(ep0);
            }

            Assert.NotNull(await ep.SelectOneAsync(selector, CancellationToken.None));
        }
    }
}
