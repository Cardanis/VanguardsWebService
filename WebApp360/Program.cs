using System;
using System.Collections.Generic;
using System.IdentityModel.Selectors;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Security;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;

namespace WebApp360
{
    class Program
    {
        static void Main(string[] args)
        {
            WebServiceHost host = new WebServiceHost(
                typeof(VanguardsServices), new Uri("http://localhost:8080/Vanguards"));
            WebHttpBinding binding = new WebHttpBinding();
            //binding.Security.Mode = WebHttpSecurityMode.TransportCredentialOnly;
            //binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;
            ServiceEndpoint ep = host.AddServiceEndpoint(typeof(IVanguardsServices), binding, "");
            
            //host.Credentials.UserNameAuthentication.UserNamePasswordValidationMode = UserNamePasswordValidationMode.Custom;
            //host.Credentials.UserNameAuthentication.CustomUserNamePasswordValidator = new CustomUserNamePasswordValidator();

            
            //If you're getting an addressdeniedexception here, VisualStudio needs to be run in administrator mode 
            //(Basically Windows is stopping it from binding on that socket)
            host.Open();
            Console.WriteLine(host.Authentication.AuthenticationSchemes.ToString());
            Console.WriteLine("Service opened on {0}", host.BaseAddresses[0]);            
            Console.ReadLine();
            host.Close();
        }

        public class CustomUserNamePasswordValidator : UserNamePasswordValidator
        {

            public override void Validate(string userName, string password)
            {
                if (null == userName || null == password)
                {
                    throw new FaultException("Empty Username or Password");
                }
                if (!userName.Equals("Admin") && !userName.Equals("admin"))
                {
                    throw new FaultException("Unknown Username or Password");
                }
                Console.WriteLine("User: " + userName + " was authenticated");
            }
        }
    }
}
