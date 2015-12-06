using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
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
            
            //If you're getting an addressdeniedexception here, VisualStudio needs to be run in administrator mode 
            //(Basically Windows is stopping it from binding on that socket)
            host.Open();
            Console.WriteLine("Service opened on {0}", host.BaseAddresses[0]);            
            Console.ReadLine();
            host.Close();
        }
    }
}
