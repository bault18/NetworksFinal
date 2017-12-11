using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace NetworksFinal
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            HeatMapInstructions.Text = "To see a heat-map of your internet traffic press the button below.";
        }

        //Loads trace route map of user input IP/Domain/URL
        private void submit_btn_Click(object sender, RoutedEventArgs e)
        {
            data.Text = String.Empty;
            Path Locations = new Path();
            runTraceRT(WebAddress.Text, ref Locations);
            Locations.LoadPolylineMap();
        }

        //Loads heat-map of computer's DNS cache
        private void getHeatData_btn_Click(object sender, RoutedEventArgs e)
        {
            //Grab DNS cache
            string DisplayDNSData = getCachedDNS();
            List<string> IPAddresses = new List<string>();

            //Parse DNS cache
            for(int i = 0; i < DisplayDNSData.Length - 11; i++)
            {
                if (IPAddresses.Count > 15)
                    break;
                string sub = DisplayDNSData.Substring(i, 11);
                
                if (sub == "Record Name")
                {
                    i += 24; //length of "Record Name . . . . . : "
                    for(int j = i; true; j++)
                    {
                        if(DisplayDNSData[j] == '\n')
                        {
                            IPAddresses.Add(DisplayDNSData.Substring(i, j - i - 1) + " ");
                            i = j;
                            break;
                        }
                    }
                }
            }

            //For more diverse data uncomment following line
            //IPAddresses = IPAddresses.Distinct().ToList();

            //BEGIN: Running tracert on all domains
            List<Thread> threads = new List<Thread>();
            Path Locs = new NetworksFinal.Path();
            

            //create threads
            data.Text = "Domains mapped: \n";
            foreach (string DestIP in IPAddresses)
            {
                threads.Add(new Thread(delegate () { runTraceRT(DestIP, ref Locs); }));
                data.Text += DestIP + Environment.NewLine;
            }

            
            //Start each thread
            foreach (Thread currThread in threads)
                currThread.Start();

            //Due to the limitations on # of requests per minute I need to kill my threads before 150 requests are made
            //If unlimited # of requests, change while loop condition to 'threads.Count != 0'
            while (Locs.getNumCoords() < 75 && threads.Count != 0)
            {
                for (int j = 0; j < threads.Count; j++)
                {
                    if (threads[j].IsAlive == false)
                    {
                        threads.RemoveAt(j);
                    }
                }
            }

            //Abort remaining threads
            foreach (Thread currThread in threads)
                currThread.Abort();

            Locs.LoadHeatmap();

        }
        
        /// <summary>
        /// Pulls computer's DNS cache and returns it as a string
        /// </summary>
        /// <returns>results from ipconfig /displaydns as a string</returns>
        private string getCachedDNS()
        {
            
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();

            //Setup coommand info
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/c ipconfig /displaydns";    //Run displaydns command
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;

            //Execute process
            process.StartInfo = startInfo;
            process.Start();
            string output = process.StandardOutput.ReadToEnd(); //Save process output
            process.WaitForExit();

            return output;
        }

        /// <summary>
        /// Runs tracert on given IP/URL/Domain
        /// </summary>
        /// <param name="DestIP">IP/URL/Domain to run tracert on</param>
        /// <param name="Locations">Object to store coordinate locations of each node in the trace route</param>
        void runTraceRT(string DestIP, ref Path Locations)
        {
            //Run Tracert on given destination IP
            IEnumerable<IPAddress> TraceRT = TraceRoute.GetTraceRoute(DestIP.Trim());

            //Get all path node's coordinate locations
            foreach (var CurrNodeIP in TraceRT)
            {
                Tuple<double, double> Coord = GetCoordinates(Convert.ToString(CurrNodeIP));
                if (Coord.Item1 != 0 && Coord.Item2 != 0)
                    Locations.AddLoc(Coord.Item1, Coord.Item2);
            }
        }
        
        /// <summary>
        /// Uses ip-api.com to grab data on given URL/Domain/IP and parses it for the lat,lng point
        /// </summary>
        /// <param name="url">URL/Domain/IP that coordinates need to be grabbed</param>
        /// <returns>Tuple of (lat,lng)</returns>
        private Tuple<double,double> GetCoordinates(string url)
        {
            //Grab JSON data for current IP/Url/Domain from the ip-api website
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://ip-api.com/json/" + url); //API Documentation: http://ip-api.com/docs/api:json
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            string IP_API_Response;
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                //The Newtonsoft needs brackets around the JSON to parse it
                IP_API_Response = "[" + reader.ReadToEnd() + "]";
            }

            //Parse JSON
            dynamic result = JArray.Parse(IP_API_Response);


            if (result[0].status == "success")  //if mappable IP -> add to list
            {
                double lon = Convert.ToDouble(result[0].lon);
                double lat = Convert.ToDouble(result[0].lat);
                return new Tuple<double, double>(lat, lon);
            }
            else
                return new Tuple<double, double>(0, 0); //else -> set to arbitrary lon/lat to be removed later
        }
    }
}
