using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.Net;
using System.IO;
using System.Diagnostics;

namespace NetworksFinal
{
    //All credit to Forgen on stack overflow for the TraceRoute class
    //https://stackoverflow.com/questions/142614/traceroute-and-ping-in-c-sharp
    public class TraceRoute
    {
        private const string Data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

        public static IEnumerable<IPAddress> GetTraceRoute(string hostNameOrAddress)
        {
            return GetTraceRoute(hostNameOrAddress, 1);
        }
        private static IEnumerable<IPAddress> GetTraceRoute(string hostNameOrAddress, int ttl)
        {
            Ping pinger = new Ping();
            PingOptions pingerOptions = new PingOptions(ttl, true);
            
            int timeout = 1000;
            byte[] buffer = Encoding.ASCII.GetBytes(Data);
            PingReply reply = default(PingReply);

            List<IPAddress> result = new List<IPAddress>();
            try {
                reply = pinger.Send(hostNameOrAddress, timeout, buffer, pingerOptions);
            } catch (PingException exp) {
                return result;
            }


            if (reply.Status == IPStatus.Success) {
                result.Add(reply.Address);
            }
            else if (reply.Status == IPStatus.TtlExpired || reply.Status == IPStatus.TimedOut) {
                //add the currently returned address if an address was found with this TTL
                if (reply.Status == IPStatus.TtlExpired)
                    result.Add(reply.Address);
                //recurse to get the next address...
                IEnumerable<IPAddress> tempResult = default(IEnumerable<IPAddress>);
                tempResult = GetTraceRoute(hostNameOrAddress, ttl + 1);
                result.AddRange(tempResult);
            }
            else {
                //failure 
            }


            return result;
        }
    }

    class Path
    {
        #region member variables
        //stores list of coordinate locations
        private List<Tuple<double, double>> CoordPath;
        #endregion

        //constructor
        public Path() { CoordPath = new List<Tuple<double, double>>(); }

        #region Get/Set Functions

        //OUT: # of coordinates saved in the object
        /// <summary>
        /// Returns the number off lat,lng coords currently saved
        /// </summary>
        /// <returns> # of lat,lng coords in CoordPath</returns>
        public int getNumCoords()
        { return CoordPath.Count; }

        /// <summary>
        /// Adds a set of coordinates to the object's path
        /// </summary>
        /// <param name="lat">latitude to add to path</param>
        /// <param name="lon">longitude to add to path</param>
        public void AddLoc(double lat, double lon)
        { CoordPath.Add(Tuple.Create(lat, lon)); }

        /// <summary>
        /// Grabs the first coordinate in the list and outputs the lat/lng in an str formatted for a google map pin
        /// </summary>
        /// <returns>str "lat: ____, lng: ____"</returns>
        public string getStartLocStr()
        { return "lat: " + Convert.ToString(CoordPath[0].Item1) + ", lng: " + Convert.ToString(CoordPath[0].Item2); }

        /// <summary>
        /// Grabs the last coordinate in the list and outputs the lat/lng in an str formatted for a google map pin
        /// </summary>
        /// <returns>str "lat: ____, lng: ____"</returns>
        public string getEndLocStr()
        { return "lat: " + Convert.ToString(CoordPath[CoordPath.Count - 1].Item1) + ", lng: " + Convert.ToString(CoordPath[CoordPath.Count - 1].Item2); }

        /// <summary>
        /// Converts all coordinate points in CoordPath into google polyline format
        /// Lines will be created between each adjacent item in CoordPath
        /// </summary>
        /// <returns>Google API format for polyline path</returns>
        public string getPolylineData()
        {
            string PathStr = String.Empty;
            foreach (Tuple<double, double> CurrLoc in CoordPath)
                PathStr += "{lat: " + Convert.ToString(CurrLoc.Item1) + ", lng: " + Convert.ToString(CurrLoc.Item2) + "},\n";

            //remove comma/endline from final item in str
            PathStr = PathStr.Remove(PathStr.Length - 2, 2);
            return PathStr;
        }

        /// <summary>
        /// Converts all coordinate points in CoordPath into google heat-map format
        /// </summary>
        /// <returns>Google API format for heat-map points</returns>
        public string getHeatmapData()
        {
            string PathStr = "";
            foreach (Tuple<double, double> CurrLoc in CoordPath)
                PathStr += "new google.maps.LatLng(" + Convert.ToString(CurrLoc.Item1) + ", " + Convert.ToString(CurrLoc.Item2) + "),\n";

            PathStr = PathStr.Remove(PathStr.Length - 2, 2);
            return PathStr;
        }
        #endregion

        /// <summary>
        /// Launches chrome browser displaying polyline map of coordinates in object
        /// </summary>
        public void LoadPolylineMap()
        {
            //Clear out HTML file
            string BlankDoc = AppDomain.CurrentDomain.BaseDirectory + "GoogleMaps\\BlankPolylines.html";
            string NewLoc = AppDomain.CurrentDomain.BaseDirectory + "GoogleMaps\\UpdatedPolylines.html";

            File.WriteAllText(NewLoc, String.Empty);

            if (CoordPath.Count == 0)
                Process.Start("Chrome", BlankDoc);
            else
            {
                string UpdatedHTML = File.ReadAllText(BlankDoc);
                //Add Polylines/Start/End locations
                UpdatedHTML = UpdatedHTML.Replace("*ROUTER COORDS HERE*", getPolylineData());
                UpdatedHTML = UpdatedHTML.Replace("*START COORDS HERE*", getStartLocStr());
                UpdatedHTML = UpdatedHTML.Replace("*END COORDS HERE*", getEndLocStr());

                File.WriteAllText(NewLoc, UpdatedHTML);

                //Load updated file in chrome
                Process.Start("Chrome", NewLoc);
            }
        }


        /// <summary>
        /// Launches chrome browser displaying heat-map of coordinate points in object
        /// </summary>
        public void LoadHeatmap()
        {
            //Clear out HTML file
            string BlankDoc = AppDomain.CurrentDomain.BaseDirectory + "GoogleMaps\\BlankHeatmap.html";
            string NewLoc = AppDomain.CurrentDomain.BaseDirectory + "GoogleMaps\\UpdatedHeatmap.html";

            File.WriteAllText(NewLoc, String.Empty);

            //Grab HTML file with no coordinates
            string UpdatedHTML = File.ReadAllText(BlankDoc);

            //If no coords saved -> load blank document
            if(CoordPath.Count == 0)
                Process.Start("Chrome", BlankDoc);
            else
            {
                //Add coords to file
                UpdatedHTML = UpdatedHTML.Replace("*ROUTER COORDS HERE*", getHeatmapData());
                File.WriteAllText(NewLoc, UpdatedHTML);

                //Load HTML in Chrome
                Process.Start("Chrome", NewLoc);
            }
        }
    }
}
