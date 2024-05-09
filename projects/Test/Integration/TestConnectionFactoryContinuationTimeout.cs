﻿// This source code is dual-licensed under the Apache License, version
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
using System.Threading.Tasks;
using RabbitMQ.Client;
using Xunit;
using Xunit.Abstractions;

namespace Test.Integration
{
    public class TestConnectionFactoryContinuationTimeout : IntegrationFixture
    {
        public TestConnectionFactoryContinuationTimeout(ITestOutputHelper output) : base(output)
        {
        }

        public override Task InitializeAsync()
        {
            // NB: nothing to do here since each test creates its own factory,
            // connections and channels
            Assert.Null(_connFactory);
            Assert.Null(_conn);
            Assert.Null(_channel);
            return Task.CompletedTask;
        }

        [Fact]
        public async Task TestConnectionFactoryContinuationTimeoutOnRecoveringConnection()
        {
            var continuationTimeout = TimeSpan.FromSeconds(777);
            using (IConnection c = await CreateConnectionWithContinuationTimeoutAsync(true, continuationTimeout))
            {
                using (IChannel ch = await c.CreateChannelAsync())
                {
                    Assert.Equal(continuationTimeout, ch.ContinuationTimeout);
                    await ch.CloseAsync();
                }

                await c.CloseAsync();
            }
        }

        [Fact]
        public async Task TestConnectionFactoryContinuationTimeoutOnNonRecoveringConnection()
        {
            var continuationTimeout = TimeSpan.FromSeconds(777);
            using (IConnection c = await CreateConnectionWithContinuationTimeoutAsync(false, continuationTimeout))
            {
                using (IChannel ch = await c.CreateChannelAsync())
                {
                    Assert.Equal(continuationTimeout, ch.ContinuationTimeout);
                    await ch.CloseAsync();
                }

                await c.CloseAsync();
            }
        }

        private Task<IConnection> CreateConnectionWithContinuationTimeoutAsync(bool automaticRecoveryEnabled, TimeSpan continuationTimeout)
        {
            ConnectionFactory cf = CreateConnectionFactory();
            cf.AutomaticRecoveryEnabled = automaticRecoveryEnabled;
            cf.ContinuationTimeout = continuationTimeout;
            return cf.CreateConnectionAsync();
        }
    }
}
