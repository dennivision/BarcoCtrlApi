using BarcoCtrlApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarcoCtrlClientApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var clientId = "crestron";
            var clientSecret = "EbuAr3RyLMeilul4CtjxuGsYSNs4QPzD";
            var tokenUrl = "https://ctrl-jocbarc.NYPD.finest/auth/realms/OCS/protocol/openid-connect/token";

            try
            {
                OAuth2Client auth2Client = new OAuth2Client(clientId, clientSecret, tokenUrl);

                //TokenResponse response = auth2Client.GetAccessTokenAsync().Result;

                //BarcoCtrlApiClient client = new BarcoCtrlApiClient("https://localhost:44319", auth2Client);
                // ctrl-jocbarc.NYPD.finest - 10.112.57.0
                BarcoCtrlApiClient client = new BarcoCtrlApiClient("https://ctrl-jocbarc.NYPD.finest", auth2Client);
                //BarcoCtrlApiClient client = new BarcoCtrlApiClient("https://10.112.57.0", auth2Client, response);
                string apiVersion = client.GetApiVersionAsync().GetAwaiter().GetResult();

                Console.WriteLine("API Verison is : " + apiVersion);


                List<WorkplaceDto> workplaces = client.GetWorkplacesAsync().Result;
                foreach (WorkplaceDto workplace in workplaces)
                {
                    Console.WriteLine($"Workplace: {workplace.Name}, Type: {workplace.Type}");
                    Console.WriteLine($"  Width: {workplace.WallGeometry.SizePx.Width}, Height: {workplace.WallGeometry.SizePx.Height}");
                    Console.WriteLine($"  Columns: {workplace.WallGeometry.Grid.Columns}, Rows: {workplace.WallGeometry.Grid.Rows}");
                    Console.WriteLine();
                }

                if (workplaces.Count > 0)
                {
                    WorkplaceDto workplace = client.GetWorkplaceAsync(workplaces[0].Id).Result;
                    Console.WriteLine($"Workplace[0]: {workplace.Name}, Type: {workplace.Type}");
                    Console.WriteLine($"  Width: {workplace.WallGeometry.SizePx.Width}, Height: {workplace.WallGeometry.SizePx.Height}");
                    Console.WriteLine($"  Columns: {workplace.WallGeometry.Grid.Columns}, Rows: {workplace.WallGeometry.Grid.Rows}");
                    Console.WriteLine();
                }
                else 
                {
                    Console.WriteLine("...No workplaces...");
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.GetType().Name);
                Console.WriteLine(ex.Message);
                Console.WriteLine();
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine();
            Console.WriteLine("Press Any Key to exit");
            Console.ReadKey(true);
        }
    }
}
