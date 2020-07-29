// The MIT License (MIT)
// 
// Copyright (c) 2015-2020 Rasmus Mikkelsen
// Copyright (c) 2015-2020 eBay Software Foundation
// https://github.com/eventflow/EventFlow
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;

namespace EventFlow.ReadStores
{
    public abstract class ReadModelEnvelope
    {
        protected ReadModelEnvelope(
            string readModelId,
            long? version)
        {
            if (string.IsNullOrEmpty(readModelId)) throw new ArgumentNullException(nameof(readModelId));

            ReadModelId = readModelId;
            Version = version;
        }

        public string ReadModelId { get; }
        public long? Version { get; }
    }

    public class ReadModelEnvelope<TReadModel> : ReadModelEnvelope
        where TReadModel : class, IReadModel
    {
        private ReadModelEnvelope(
            string readModelId,
            TReadModel readModel,
            long? version)
            : base(readModelId, version)
        {
            ReadModel = readModel;
        }

        public TReadModel ReadModel { get; }

        public static ReadModelEnvelope<TReadModel> Empty(string readModelId)
        {
            return new ReadModelEnvelope<TReadModel>(readModelId, null, null);
        }

        public static ReadModelEnvelope<TReadModel> With(
            string readModelId,
            TReadModel readModel)
        {
            return new ReadModelEnvelope<TReadModel>(readModelId, readModel, null);
        }

        public static ReadModelEnvelope<TReadModel> With(
            string readModelId,
            TReadModel readModel,
            long? version)
        {
            return new ReadModelEnvelope<TReadModel>(readModelId, readModel, version);
        }

        public static ReadModelEnvelope<TReadModel> With(
            string readModelId,
            TReadModel readModel,
            long version)
        {
            return new ReadModelEnvelope<TReadModel>(readModelId, readModel, version);
        }
    }
}
