using EventFlow.MongoDB.ValueObjects;
using MongoDB.Driver;

namespace EventFlow.MongoDB.EventStore
{
	using System.Threading;

	public class MongoDbEventSequenceStore : IMongoDbEventSequenceStore
	{
		private const string _collectionName = "eventflow.counter";
		private readonly IMongoDatabase _mongoDatabase;

		public MongoDbEventSequenceStore(IMongoDatabase mongoDatabase)
		{
			_mongoDatabase = mongoDatabase;
		}

		public long GetNextSequence(string name)
		{
			MongoDbCounterDataModel ret = _mongoDatabase.GetCollection<MongoDbCounterDataModel>(_collectionName)
				.FindOneAndUpdate<MongoDbCounterDataModel>(
					x => x._id == name,
					new UpdateDefinitionBuilder<MongoDbCounterDataModel>().Inc(x => x.Seq, value: 1),
					new FindOneAndUpdateOptions<MongoDbCounterDataModel> {IsUpsert = true, ReturnDocument = ReturnDocument.After});

			return ret.Seq;
		}
	}
}
