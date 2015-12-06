using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.Text;
using System.Threading.Tasks;

namespace WebApp360
{
    [ServiceBehavior]
    [AspNetCompatibilityRequirements]
    public class VanguardsServices : IVanguardsServices
    {
        private MongoClient mongoClient;
        private IMongoDatabase mongoDatabase;

        const string DATABASE_NAME = "VanguardsDatabase";

        //Collection Names
        const string TEST_COLLECTION = "TestCollection";
        public VanguardsServices() 
        {
            mongoClient = new MongoClient();
            mongoDatabase = mongoClient.GetDatabase(DATABASE_NAME);
        }

        public string GetTestValue(string id, string id2)
        {
            Console.WriteLine(id);
            //Grab the collection from the database
            IMongoCollection<BsonDocument> collection = mongoDatabase.GetCollection<BsonDocument>(id);

            //We're creating a filter which is basically like our search params
            var filter = Builders<BsonDocument>.Filter.Eq("ID", id);

            //Here we use that filter to search the collectionfor anything that matches it.  MongoDB is Asyncronous, but since we need to return the value
            //We need to wait on the result, using .Result tells this thread to wait for the Find to return.
            //Technically the Find method returns a collection of results, here I just use 'First' to get the first of those results.
            var doc = collection.Find(filter).FirstAsync().Result;
            return doc.ToString();
        }


        public string PostTestValue(string id, string id2)
        {
            //This gets the body of the post request
            string body = Encoding.UTF8.GetString(OperationContext.Current.RequestContext.RequestMessage.GetBody<byte[]>());

            //This creates the document that we're going to put in the collection.  Each BsonElement is a key/value pair that gets put in the json
            BsonDocument doc = new BsonDocument(new List<BsonElement> { new BsonElement("ID", id), new BsonElement("BODY", body) });

            IMongoCollection<BsonDocument> collection = mongoDatabase.GetCollection<BsonDocument>(id);
            collection.InsertOneAsync(doc).Wait();
            return "POSTED!";
        }
    }
}
