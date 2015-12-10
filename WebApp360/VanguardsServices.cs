using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Web;

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
        const string GAMES_LIST_COLLECTION = "GamesListCollection";

        const string ENCODING_KEY = "Encoder";
        const string ENCODING_TYPE_BYTESTREAM = "ByteStreamMessageEncoder";
        const string ENCODING_TYPE_PLAIN_JSON = "application/json; charset=utf-8";
        const string ENCODING_UE4_PROPERTY_KEY = "Via";
        
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
            string body = OperationContext.Current.RequestContext.RequestMessage.ToString();

            //This creates the document that we're going to put in the collection.  Each BsonElement is a key/value pair that gets put in the json
            BsonDocument doc = new BsonDocument(new List<BsonElement> { new BsonElement("ID", id), new BsonElement("BODY", body) });

            IMongoCollection<BsonDocument> collection = mongoDatabase.GetCollection<BsonDocument>(id);
            collection.InsertOneAsync(doc).Wait();
            return "Posted: " + body;
        }


        public string GetGamesList()
        {
            //Grab the collection from the database
            try
            {
                IMongoCollection<BsonDocument> collection = mongoDatabase.GetCollection<BsonDocument>(GAMES_LIST_COLLECTION);

                List<BsonDocument> docs = collection.Find(Builders<BsonDocument>.Filter.Empty).ToListAsync().Result;
                return BsonExtensionMethods.ToJson(docs);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return e.Message;
            }
        }


        public string PostGameInfo()
        {
            string bodyString = string.Empty;
            try
            {
                Message m = OperationContext.Current.RequestContext.RequestMessage;
                bodyString = ParseBodyFromMessage(m);
                Console.WriteLine(bodyString);
                //bodyString = new StreamReader(body).ReadToEnd();
                Console.WriteLine(bodyString);
                
                //This creates the document that we're going to put in the collection.  Each BsonElement is a key/value pair that gets put in the json
                BsonDocument doc = new BsonDocument(new List<BsonElement> { new BsonElement("BODY", bodyString) });
                doc = BsonDocument.Parse(bodyString);
                IMongoCollection<BsonDocument> collection = mongoDatabase.GetCollection<BsonDocument>(GAMES_LIST_COLLECTION);
                collection.InsertOneAsync(doc).Wait(5000);
                return "Posted: " + bodyString;
            }
            catch (Exception e)
            {
                Console.WriteLine(bodyString);
                Console.WriteLine(e.Message);
                return e.Message;
            }
        }

        static string ParseBodyFromMessage(Message m)
        {
            if (m.Properties.ContainsKey(ENCODING_KEY))
            {
                string encodingType = m.Properties[ENCODING_KEY].ToString();
                if (encodingType.Equals(ENCODING_TYPE_BYTESTREAM))
                {
                    return Encoding.UTF8.GetString(m.GetBody<byte[]>());
                }
                else if (encodingType.Equals(ENCODING_TYPE_PLAIN_JSON))
                {
                    string incoming = m.ToString();
                    throw new Exception("Handling JSON isn't supported cause C# is being dumb, use plaintext instead");
                }
            }
            else if(m.Properties.ContainsKey(ENCODING_UE4_PROPERTY_KEY))
            {
                string encoded = m.Properties[ENCODING_UE4_PROPERTY_KEY].ToString();
                encoded = encoded.Split('?')[1];
                NameValueCollection col = HttpUtility.ParseQueryString(encoded);
                List<BsonElement> build = new List<BsonElement>();
                Console.WriteLine(col.ToString() + " " + col.Count);
                foreach (string key in col.Keys)
                {
                    Console.WriteLine("ADDING: " + key + " " + col[key]);
                    build.Add(new BsonElement(key, col[key]));
                }
                BsonDocument doc = new BsonDocument(build);
                return doc.ToString();//BsonExtensionMethods.ToJson(build);
            }
            return null;
        }
    }
}
