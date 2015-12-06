using System;
using System.Collections.Generic;
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
        [WebGet(UriTemplate="Test/GetTestValue?id={id}&placeholder={id2}")]
        string GetTestValue(string id, string id2);

        [OperationContract]
        [WebInvoke(UriTemplate="Test/PostTestValue?id={id}&placeholder={id2}")]
        string PostTestValue(string id, string id2);
    }
}
