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
using System.ServiceModel.Web;
using System.Text;
using System.Text.RegularExpressions;
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
        const string USERS_COLLECTION = "UsersCollection";
        const string ABILITY_SETS_USER = "AbilitySetsUser";
        const string ABILITY_SETS_GLOBAL = "AbilitySetsGlobal";
        const string DEATH_INFO_COLLECTION = "DeathInfoCollection";
        const string AUTH_TOKENS = "AuthTokens";

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
                WebOperationContext.Current.OutgoingResponse.Headers
                    .Add("Access-Control-Allow-Origin", "*");
                List<BsonDocument> docs = collection.Find(Builders<BsonDocument>.Filter.Empty).ToListAsync().Result;
                //OperationContext.Current.OutgoingMessageHeaders.Add(MessageHeader.CreateHeader("Access-Control-Allow-Origin", "", "*"));
                //OperationContext.Current.OutgoingMessageProperties.Add("Access-Control-Allow-Origin", "*");
                string json = ScrubIdsFromData(BsonExtensionMethods.ToJson(docs));
                Console.WriteLine("SCRUBBED: " + json);
                return json;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return e.Message;
            }
        }


        public string PostGameInfo(Stream stream)
        {
            string bodyString = string.Empty;
            try
            {
                bodyString = new StreamReader(stream).ReadToEnd();
                Console.WriteLine(bodyString);                
                //This creates the document that we're going to put in the collection.  Each BsonElement is a key/value pair that gets put in the json
                BsonDocument doc = BsonDocument.Parse(bodyString);
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

        public string CreateUser(Stream stream)
        {
            string bodyString = string.Empty;
            try
            {
                bodyString = new StreamReader(stream).ReadToEnd();
                Console.WriteLine(bodyString);
                NameValueCollection nameValues = HttpUtility.ParseQueryString(bodyString);

                BsonDocument doc = new BsonDocument();
                foreach (string key in nameValues.AllKeys)
                {
                    doc.Add(new BsonElement(key, nameValues[key])); 
                }
                
                IMongoCollection<BsonDocument> collection = mongoDatabase.GetCollection<BsonDocument>(USERS_COLLECTION);

                var filter = Builders<BsonDocument>.Filter.Eq("username", nameValues["username"]);

                var result = collection.Find(filter).CountAsync().Result;

                WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Origin", "*");

                if (result <= 0)
                {
                    WebOperationContext.Current.OutgoingResponse.Headers.Add("Status", "403");
                    return "Failed to create user. Username already taken.";
                }

                collection.InsertOneAsync(doc).Wait(5000);

                return "UserCreated";
            }
            catch (Exception e)
            {
                Console.WriteLine(bodyString);
                Console.WriteLine(e.Message);
                return e.Message;
            }
        }

        public string Login(string username, string password)
        {
            string bodyString = string.Empty;
            try
            {
                IMongoCollection<BsonDocument> collection = mongoDatabase.GetCollection<BsonDocument>(USERS_COLLECTION);
                var filter = Builders<BsonDocument>.Filter.Eq("User.username", username);
                var filter2 = Builders<BsonDocument>.Filter.Eq("User.password", password);
                var combinedFilter = filter & filter2;
                var result = collection.Find(combinedFilter).FirstAsync().Result;

                Console.WriteLine(bodyString + " " + result.ToString());
                WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Origin", "*");

                if (result < 1)
                {
                    WebOperationContext.Current.OutgoingResponse.Headers.Add("Status", "403");
                    return "Login Failed";
                }

                return result.ToString();
            }
            catch(Exception e)
            {
                Console.WriteLine(bodyString);
                Console.WriteLine(e.Message);
                return e.Message;
            }
        }

        public string UsernameForAuthToken(string AuthToken)
        {
            IMongoCollection<BsonDocument> collection = mongoDatabase.GetCollection<BsonDocument>(USERS_COLLECTION);
            return "";
        }

        /// <summary>
        /// Schema for User specific picks:
        /// {
        ///     Username : {username}
        ///     AbilitySets : [
        ///                     {Ability1 : {ability1}, Ability2:{ability2}, Ability3:{ability3},Ability4:{ability4},TimesPicked:{num},LastPicked:{date}
        ///                ]
        ///     SingleAbilityPicks : [
        ///                             {Ability:{name},TimesPicked:{name},LastPicked:{date}}
        ///                         ]
        /// }
        /// 
        /// Schema for global:
        /// {
        ///     
        ///      {Ability1 : {ability1}, Ability2:{ability2}, Ability3:{ability3},Ability4:{ability4},TimesPicked:{num},LastPicked:{date}}
        ///      or
        ///      {Ability:{name},TimesPicked:{name},LastPicked:{date}}
        ///  
        /// }
        /// 
        /// Body expected to be:
        /// {
        ///     Ability1:{name},Ability2:{name},Ability3:{name},Ability4:{name},Date:{date},Username:{username}
        /// }
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public string PostAbilityData(Stream stream)
        {
            string bodyString = string.Empty;
            try
            {
                bodyString = new StreamReader(stream).ReadToEnd();
                BsonDocument doc = BsonDocument.Parse(bodyString);
                doc = doc.GetValue("AbilityInfo").AsBsonDocument;
                string username = doc.GetValue("Username").AsString;
                string ability1 = doc.GetValue("Ability1").AsString;
                string ability2 = doc.GetValue("Ability2").AsString;
                string ability3 = doc.GetValue("Ability3").AsString;
                string ability4 = doc.GetValue("Ability4").AsString;
                string date = doc.GetValue("Date").AsString;

                IMongoCollection<BsonDocument> userCollection = mongoDatabase.GetCollection<BsonDocument>(ABILITY_SETS_USER);
                FilterDefinition<BsonDocument> filterForUser = Builders<BsonDocument>.Filter.Eq("Username", username);

                long existingUser = userCollection.CountAsync(filterForUser).Result;
                if (existingUser == 0)
                {
                    BsonDocument userDoc = new BsonDocument(new List<BsonElement>
                        {
                            new BsonElement("Username", username), 
                            new BsonElement("AbilitySets", new BsonArray()), 
                            new BsonElement("SingleAbilityPicks", new BsonArray())
                        }
                    );
                    Console.WriteLine("No user found, inserting: " + userDoc.ToJson());
                    userCollection.InsertOneAsync(userDoc).Wait();
                }
                FilterDefinition<BsonDocument> matchinAbility1 = Builders<BsonDocument>.Filter.Eq("AbilitySets.Ability1", ability1);
                FilterDefinition<BsonDocument> matchinAbility2 = Builders<BsonDocument>.Filter.Eq("AbilitySets.Ability2", ability2);
                FilterDefinition<BsonDocument> matchinAbility3 = Builders<BsonDocument>.Filter.Eq("AbilitySets.Ability3", ability3);
                FilterDefinition<BsonDocument> matchinAbility4 = Builders<BsonDocument>.Filter.Eq("AbilitySets.Ability4", ability4);

                FilterDefinition<BsonDocument> matchinAbilitySet =
                    matchinAbility1 &
                    matchinAbility2 &
                    matchinAbility3 &
                    matchinAbility4;

                long existingDefinition = userCollection.Find(matchinAbilitySet & filterForUser).CountAsync().Result;
                if (existingDefinition > 0)
                {
                    UpdateDefinition<BsonDocument> inc = Builders<BsonDocument>.Update.Inc("AbilitySets.$.TimesPicked", 1);
                    UpdateDefinition<BsonDocument> set = Builders<BsonDocument>.Update.Set("AbilitySets.$.LastPicked", date);
                    
                    userCollection.UpdateOneAsync(matchinAbilitySet & filterForUser, inc);
                    userCollection.UpdateOneAsync(matchinAbilitySet & filterForUser, set);
                    Console.WriteLine("Ability definition exists, updating");
                }
                else
                {
                    //Not found, create new
                    BsonDocument newRow = new BsonDocument(new List<BsonElement>{
                        new BsonElement("Ability1", ability1),
                        new BsonElement("Ability2", ability2),
                        new BsonElement("Ability3", ability3),
                        new BsonElement("Ability4", ability4),
                        new BsonElement("TimesPicked", 1),
                        new BsonElement("LastPicked", date)
                    });
                    userCollection.FindOneAndUpdateAsync(filterForUser,
                        Builders<BsonDocument>.Update.Push("AbilitySets", newRow));
                    Console.WriteLine("Ability definition doesnt exist, creating " + newRow.ToJson());
                }

                FilterDefinition<BsonDocument>[] matchSingleAbilities = new FilterDefinition<BsonDocument>[]{
                    Builders<BsonDocument>.Filter.Eq("SingleAbilityPicks.Ability", ability1),
                    Builders<BsonDocument>.Filter.Eq("SingleAbilityPicks.Ability", ability2),
                    Builders<BsonDocument>.Filter.Eq("SingleAbilityPicks.Ability", ability3),
                    Builders<BsonDocument>.Filter.Eq("SingleAbilityPicks.Ability", ability4)
                };
                string[] singleAbilities = new string[]{
                    ability1, ability2, ability3, ability4
                };

                for (int i = 0; i < 4; i++)
                {
                    FilterDefinition<BsonDocument> filter = matchSingleAbilities[i];
                    string name = singleAbilities[i];
                    long existing = userCollection.Find(filter & filterForUser).CountAsync().Result;

                    if (existing > 0)
                    {
                        Console.WriteLine("Single ability present, updating");
                        UpdateDefinition<BsonDocument> inc = Builders<BsonDocument>.Update.Inc("SingleAbilityPicks.$.TimesPicked", 1);
                        UpdateDefinition<BsonDocument> set = Builders<BsonDocument>.Update.Set("SingleAbilityPicks.$.LastPicked", date);

                        userCollection.UpdateOneAsync(filter & filterForUser, inc);
                        userCollection.UpdateOneAsync(filter & filterForUser, set);
                    }
                    else
                    {
                        Console.WriteLine("Single ability missing");
                        //Not found, create new
                        BsonDocument newRow = new BsonDocument(new List<BsonElement>{
                            new BsonElement("Ability", name),
                            new BsonElement("TimesPicked", 1),
                            new BsonElement("LastPicked", date)
                        });
                        userCollection.FindOneAndUpdateAsync(filterForUser,
                            Builders<BsonDocument>.Update.Push("SingleAbilityPicks", newRow));
                    }
                }

                IMongoCollection<BsonDocument> globalCollection = mongoDatabase.GetCollection<BsonDocument>(ABILITY_SETS_GLOBAL);

                return "AbilityDataPosted";
            }
            catch (Exception e)
            {
                Console.WriteLine(bodyString);
                Console.WriteLine(e.Message);
                return e.Message;
            }
        }

        public string RawPlayerAbilityData()
        {
            //Grab the collection from the database
            try
            {
                IMongoCollection<BsonDocument> collection = mongoDatabase.GetCollection<BsonDocument>(ABILITY_SETS_USER);
                WebOperationContext.Current.OutgoingResponse.Headers
                    .Add("Access-Control-Allow-Origin", "*");
                List<BsonDocument> docs = collection.Find(Builders<BsonDocument>.Filter.Empty).ToListAsync().Result;
                //OperationContext.Current.OutgoingMessageHeaders.Add(MessageHeader.CreateHeader("Access-Control-Allow-Origin", "", "*"));
                //OperationContext.Current.OutgoingMessageProperties.Add("Access-Control-Allow-Origin", "*");
                string json = ScrubIdsFromData(BsonExtensionMethods.ToJson(docs));
                Console.WriteLine("SCRUBBED: " + json);
                return json;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return e.Message;
            }
        }

        public Stream DownloadGame()
        {
            try
            {
                string fileName = "Vanguards.zip";
                string fullPath = Path.GetFullPath(fileName);
                Console.WriteLine("Path is: " + fullPath);
                WebOperationContext.Current.OutgoingResponse.ContentType = "application/octet-stream";
                WebOperationContext.Current.OutgoingResponse.Headers.Add("X-Filename", fileName);
                WebOperationContext.Current.OutgoingResponse.Headers.Add("Content-Disposition: attachment; filename=\"Vanguards.zip\"");
                return File.OpenRead(fullPath);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }

        }

        public string PostDeathInfo(Stream stream)
        {
            string bodyString = string.Empty;
            try
            {
                bodyString = new StreamReader(stream).ReadToEnd();
                Console.WriteLine(bodyString);
                BsonDocument doc = BsonDocument.Parse(bodyString);
                IMongoCollection<BsonDocument> collection = mongoDatabase.GetCollection<BsonDocument>(DEATH_INFO_COLLECTION);

                collection.InsertOneAsync(doc).Wait(5000);
                WebOperationContext.Current.OutgoingResponse.Headers
                    .Add("Access-Control-Allow-Origin", "*");
                return "Death Info stored";
            }
            catch (Exception e)
            {
                Console.WriteLine(bodyString);
                Console.WriteLine(e.Message);
                return e.Message;
            }
        }

        public string GetDeathInfo()
        {
            IMongoCollection<BsonDocument> collection = mongoDatabase.GetCollection<BsonDocument>(DEATH_INFO_COLLECTION);
            List<BsonDocument> docs = collection.Find(Builders<BsonDocument>.Filter.Empty).ToListAsync().Result;
            BsonArray arr = new BsonArray();
            Console.WriteLine(arr.ToJson());
            foreach (BsonDocument doc in docs)
            {
                Console.WriteLine(doc.ToJson());
                arr.Add(doc["DeathInfo"].AsBsonDocument["DeathLocation"]);
            }
            WebOperationContext.Current.OutgoingResponse.Headers
                    .Add("Access-Control-Allow-Origin", "*");
            Console.WriteLine(arr.ToJson());
            return arr.ToJson();
        }

        static string ScrubIdsFromData(string data)
        {
            
            string sharedPattern = "(ObjectId[(][\\][\"]([a-zA-Z0-9]*)[\\][\"][)])";
            string innerPattern = "[\\][\"][a-zA-Z0-9]*[\\][\"]";
            Regex regOuter = new Regex(sharedPattern);
            Regex regInner = new Regex(innerPattern);

            string newData = data;
            Match outerMatch = regOuter.Match(data);
            while (outerMatch.Success)
            {
                string capturedVal = outerMatch.Captures[0].Value;
                Match innerMatch = regInner.Match(capturedVal);

                newData = newData.Replace(outerMatch.ToString(), innerMatch.ToString());

                outerMatch = outerMatch.NextMatch();
            }
            return newData;
        }

        static string ParseBodyMessageAttempt2(WebOperationContext ctx)
        {
            Console.WriteLine(ctx.IncomingRequest.ContentLength + " " + ctx.IncomingRequest.ContentType);
            foreach (string s in ctx.IncomingRequest.Headers.AllKeys)
            {
                Console.WriteLine(s + "+++" + ctx.IncomingRequest.Headers[s]);
            }
            string val = ctx.IncomingRequest.ToString();
            Console.WriteLine(val);
            return val;
        }

        static string ParseBodyFromMessage(Message m)
        {
            foreach (string key in m.Properties.Keys)
            {
                Console.WriteLine(key + "---" + m.Properties[key]);
            }
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
            else if(m.Properties.ContainsKey(ENCODING_UE4_PROPERTY_KEY) && m.Properties[ENCODING_UE4_PROPERTY_KEY].ToString().Contains('?'))
            {
                string encoded = m.Properties[ENCODING_UE4_PROPERTY_KEY].ToString();
                Console.WriteLine("RECEIVED: " + encoded);
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
