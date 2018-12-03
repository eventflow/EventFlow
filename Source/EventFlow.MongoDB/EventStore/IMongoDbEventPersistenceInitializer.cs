using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventFlow.MongoDB.EventStore
{
    public interface IMongoDbEventPersistenceInitializer
    {
        void Initialize();
    }
}
