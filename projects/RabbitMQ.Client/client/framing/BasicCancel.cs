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

using RabbitMQ.Client.client.framing;
using RabbitMQ.Client.Impl;

namespace RabbitMQ.Client.Framing.Impl
{
    internal readonly struct BasicCancel : IOutgoingAmqpMethod
    {
        public readonly ConsumerTag _consumerTag;
        public readonly bool _noWait;

        public BasicCancel(ConsumerTag consumerTag, bool noWait)
        {
            _consumerTag = consumerTag;
            _noWait = noWait;
        }

        public ProtocolCommandId ProtocolCommandId => ProtocolCommandId.BasicCancel;

        public int WriteTo(Span<byte> span)
        {
            int offset = WireFormatting.WriteShortstr(ref span.GetStart(), (ReadOnlySpan<byte>)_consumerTag);
            return offset + WireFormatting.WriteBits(ref span.GetOffset(offset), _noWait);
        }

        public int GetRequiredBufferSize()
        {
            int bufferSize = 1 + 1; // bytes for length of _consumerTag, bit fields
            bufferSize += _consumerTag.Length; // _consumerTag in bytes
            return bufferSize;
        }

        public static ConsumerTag GetConsumerTag(ReadOnlyMemory<byte> data)
        {
            WireFormatting.ReadShortMemory(data, out ReadOnlyMemory<byte> consumerTagMemory);
            return new ConsumerTag(consumerTagMemory);
        }
    }
}
