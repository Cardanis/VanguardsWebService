using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;

namespace WebApp360
{
    [ServiceContract]
    public interface IVanguardsServices
    {
        //Here is where you'll define your own REST endpoints
        //They all need to have [OperationContract] above them
        //Also, you need either WebGet (for get) or WebInvoke (for post) you shouldn't need any others.
        //The UriTemplate value defines what the URL will look like to access this method 
        //  (append this on to the URI used to host in in the Program.cs file)
        //For GetTestValue, it would pass in {id} to the id parameter and {id2} to the id2 parameter (makes sense right?)
        

        [OperationContract]
        [WebGet(UriTemplate="Test/GetTestValue?id={id}&placeholder={id2}", 
            RequestFormat=WebMessageFormat.Json, ResponseFormat=WebMessageFormat.Json)]
        string GetTestValue(string id, string id2);

        [OperationContract]
        [WebInvoke(UriTemplate = "Test/PostTestValue?id={id}&placeholder={id2}",
            RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        string PostTestValue(string id, string id2);

        [OperationContract]
        [WebGet(UriTemplate = "GetGamesList", ResponseFormat=WebMessageFormat.Json)]
        string GetGamesList();

        [OperationContract]
        [WebInvoke(UriTemplate = "PostGameInfo", RequestFormat=WebMessageFormat.Json, BodyStyle=WebMessageBodyStyle.Bare)]
        string PostGameInfo(Stream stream);

        [OperationContract]
        [WebInvoke(UriTemplate = "CreateUser", RequestFormat = WebMessageFormat.Json)]
        string CreateUser(Stream stream);

        [OperationContract]
        [WebGet(UriTemplate = "Login?username={username}&password={password}", ResponseFormat = WebMessageFormat.Json)]
        string Login(string username, string password);

        [OperationContract]
        [WebInvoke(UriTemplate = "PostAbilityData")]
        string PostAbilityData(Stream stream);

        [OperationContract]
        [WebGet(UriTemplate = "RawPlayerAbilityData", ResponseFormat = WebMessageFormat.Json)]
        string RawPlayerAbilityData();

        [OperationContract]
        [WebGet(UriTemplate = "DownloadGame", BodyStyle=WebMessageBodyStyle.Bare)]
        Stream DownloadGame();

        [OperationContract]
        [WebInvoke(UriTemplate = "PostDeathInfo")]
        string PostDeathInfo(Stream stream);

        [OperationContract]
        [WebGet(UriTemplate = "GetDeathInfo")]
        string GetDeathInfo();
    }
}
