﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace RuckZuck.Base
{
    public class AddSoftware
    {
        public string Architecture { get; set; }
        public string Author { get; set; }
        public string Category { get; set; }
        public string ContentID { get; set; }
        public string Description { get; set; }
        public List<contentFiles> Files { get; set; }
        public string IconHash { get; set; }
        public string IconURL
        {
            get
            {
                //Support new V2 REST API
                if (!string.IsNullOrEmpty(IconHash))
                {
                    return RZRestAPIv2.sURL + "/rest/v2/GetIcon?size=32&iconhash=" + IconHash;
                }

                if (SWId > 0)
                {
                    //IconId = SWId;
                    string sURL = RZRestAPIv2.sURL + "/rest/v2/GetIcon?size=32&iconid=" + SWId.ToString();
                    return sURL;
                }

                return "";
            }
        }

        public byte[] Image { get; set; }
        public string Manufacturer { get; set; }
        public string MSIProductID { get; set; }
        public string[] PreRequisites { get; set; }
        public string ProductName { get; set; }
        public string ProductURL { get; set; }
        public string ProductVersion { get; set; }
        public string PSDetection { get; set; }
        public string PSInstall { get; set; }
        public string PSPostInstall { get; set; }
        public string PSPreInstall { get; set; }
        public string PSPreReq { get; set; }
        public string PSUninstall { get; set; }
        public string ShortName { get; set; }
        //vNext 5.9.2017
        //public long SWId { get { return IconId; } set { IconId = value; } }
        public long SWId { get; set; }

        //public long IconId { get; set; }
        //remove if SWId is in place 5.9.2017
        //public long IconId { get; set; }
    }

    public class contentFiles
    {
        public string FileHash { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public string HashType { get; set; }
        public string URL { get; set; }
    }

    public class DLStatus
    {
        public long DownloadedBytes { get; set; }
        public string Filename { get; set; }

        public int PercentDownloaded { get; set; }
        public long TotalBytes { get; set; }
        public string URL { get; set; }
    }

    public class DLTask
    {
        internal string _status = "";
        public bool AutoInstall { get; set; }
        public long DownloadedBytes { get; set; }
        public bool Downloading { get; set; }
        public bool Error { get; set; }
        public string ErrorMessage { get; set; }
        public List<contentFiles> Files { get; set; }
        public string IconURL { get; set; }
        //public byte[] Image { get; set; }
        public bool Installed { get; set; }

        public bool Installing { get; set; }
        public string Manufacturer { get; set; }
        public int PercentDownloaded { get; set; }
        public string ProductName { get; set; }

        public string ProductVersion { get; set; }
        public string ShortName { get; set; }
        public string Status
        {
            get
            {
                if (string.IsNullOrEmpty(_status))
                {
                    if (Installing && !Error)
                        return "Installing";
                    if (Downloading && !Error)
                        return "Downloading";
                    if (Installed && !Error)
                        return "Installed";
                    if (UnInstalled && !Error)
                        return "Uninstalled";
                    if (WaitingForDependency)
                        return "Installing dependencies";
                    if (PercentDownloaded == 100 && !Error)
                        return "Downloaded";
                    if (Error)
                        return ErrorMessage;

                    return "Waiting";
                }
                else
                    return _status;
            }
            set
            {
                _status = value;
            }
        }

        public RZUpdate.SWUpdate SWUpd { get; set; }
        public long TotalBytes { get; set; }
        public bool UnInstalled { get; set; }
        public bool WaitingForDependency { get; set; }
        //public Task task { get; set; }
    }

    public class GetSoftware
    {
        public List<string> Categories { get; set; }
        public string Description { get; set; }
        public Int32? Downloads { get; set; }
        public string IconHash { get; set; }
        public string IconURL
        {
            get
            {
                //Support new V2 REST API
                if (!string.IsNullOrEmpty(IconHash))
                {
                    return RZRestAPIv2.sURL + "/rest/v2/GetIcon?size=32&iconhash=" + IconHash;
                }

                if (SWId > 0)
                {
                    return RZRestAPIv2.sURL + "/rest/GetIcon?size=32&id=" + SWId.ToString();
                }

                return "";
            }
        }

        public bool isInstalled { get; set; }
        public string Manufacturer { get; set; }
        public string ProductName { get; set; }
        public string ProductURL { get; set; }
        public string ProductVersion { get; set; }
        public string ShortName { get; set; }
        public long SWId { get; set; }

    }

    class RZRestAPIv2
    {
        public static string CustomerID = "";
        public static bool DisableBroadcast = false;
        private static string _sURL = "UDP";
        private static HttpClient oClient = new HttpClient(); //thx https://aspnetmonsters.com/2016/08/2016-08-27-httpclientwrong/

        public static string sURL
        {
            get
            {
                if (_sURL != "UDP" && !string.IsNullOrEmpty(_sURL))
                    return _sURL;

                try
                {
                    var vBroadcast = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\RuckZuck", "Broadcast", 1);
                    if ((int)vBroadcast == 0) //only disable if set to 0
                        DisableBroadcast = true;
                }
                catch { }


                if (DisableBroadcast)
                    _sURL = "";

                try
                {
                    string sCustID = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\RuckZuck", "CustomerID", "") as string;
                    if (!string.IsNullOrEmpty(sCustID))
                    {
                        CustomerID = sCustID; //Override CustomerID
                    }
                }
                catch { }

                try
                {
                    string sWebSVC = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\RuckZuck", "WebService", "") as string;
                    if (!string.IsNullOrEmpty(sWebSVC))
                    {
                        if (sWebSVC.StartsWith("http", StringComparison.CurrentCultureIgnoreCase))
                        {
                            _sURL = sWebSVC.TrimEnd('/');
                        }
                    }
                }
                catch { }

                if (_sURL == "UDP" && !DisableBroadcast)
                {
                    try
                    {
                        using (var Client = new UdpClient())
                        {
                            Client.Client.SendTimeout = 1000;
                            Client.Client.ReceiveTimeout = 1000;
                            var RequestData = Encoding.ASCII.GetBytes(Environment.MachineName);
                            var ServerEp = new IPEndPoint(IPAddress.Any, 0);

                            Client.EnableBroadcast = true;
                            Client.Send(RequestData, RequestData.Length, new IPEndPoint(IPAddress.Broadcast, 5001));

                            var ServerResponseData = Client.Receive(ref ServerEp);
                            var ServerResponse = Encoding.ASCII.GetString(ServerResponseData);
                            Console.WriteLine("Recived {0} from {1}", ServerResponse, ServerEp.Address.ToString());
                            if (ServerResponse.StartsWith("http"))
                                _sURL = ServerResponse;
                            Client.Close();
                        }
                    }
                    catch { _sURL = ""; }
                }

                if (string.IsNullOrEmpty(_sURL))
                {
                    return GetURL(CustomerID);
                }
                else
                    return _sURL;
            }
            set
            {
                _sURL = value;
            }
        }

        public static async Task<List<AddSoftware>> CheckForUpdateAsync(List<AddSoftware> lSoftware, string customerid = "")
        {
            try
            {
                if (lSoftware.Count > 0)
                {
                    if (string.IsNullOrEmpty(customerid))
                        customerid = CustomerID;

                    JavaScriptSerializer ser = new JavaScriptSerializer();
                    string sSoftware = ser.Serialize(lSoftware);
                    HttpContent oCont = new StringContent(sSoftware, Encoding.UTF8, "application/json");
                    var response = await oClient.PostAsync(sURL + "/rest/v2/checkforupdate?customerid=" + customerid, oCont);


                    List<AddSoftware> lRes = ser.Deserialize<List<AddSoftware>>(await response.Content.ReadAsStringAsync());
                    return lRes;

                    //response.Wait(180000); //3min max
                    //if (response.IsCompleted)
                    //{
                    //    List<AddSoftware> lRes = ser.Deserialize<List<AddSoftware>>(response.Result.Content.ReadAsStringAsync().Result);
                    //    return lRes;
                    //}
                }

            }
            catch
            {
                _sURL = ""; //enforce reload endpoint URL
            }

            return new List<AddSoftware>();
        }

        public static async Task<string> Feedback(string productName, string productVersion, string manufacturer, string working, string userKey, string feedback, string customerid = "")
        {
            if (!string.IsNullOrEmpty(feedback))
            {
                try
                {
                    var oRes = await oClient.GetStringAsync(sURL + "/rest/v2/feedback?name=" + WebUtility.UrlEncode(productName) + "&ver=" + WebUtility.UrlEncode(productVersion) + "&man=" + WebUtility.UrlEncode(manufacturer) + "&ok=" + working + "&user=" + WebUtility.UrlEncode(userKey) + "&text=" + WebUtility.UrlEncode(feedback) + "&customerid=" + WebUtility.UrlEncode(customerid));
                    return oRes;
                }
                catch { }
            }

            return "";
        }

        public static List<GetSoftware> GetCatalog(string customerid = "")
        {
            if (string.IsNullOrEmpty(customerid))
            {
                RZRestAPIv2.sURL.ToString();
                customerid = CustomerID;
            }

            if (!customerid.StartsWith("--"))
            {
                if (File.Exists(Path.Combine(Environment.ExpandEnvironmentVariables("%TEMP%"), "rzcat.json"))) //Cached content exists
                {
                    try
                    {
                        DateTime dCreationDate = File.GetLastWriteTime(Path.Combine(Environment.ExpandEnvironmentVariables("%TEMP%"), "rzcat.json"));
                        if ((DateTime.Now - dCreationDate) < new TimeSpan(0, 30, 0)) //Cache for 30min
                        {
                            //return cached Content
                            string jRes = File.ReadAllText(Path.Combine(Environment.ExpandEnvironmentVariables("%TEMP%"), "rzcat.json"));
                            JavaScriptSerializer ser = new JavaScriptSerializer();
                            List<GetSoftware> lRes = ser.Deserialize<List<GetSoftware>>(jRes);
                            return lRes;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("E1" + ex.Message, "GetCatalog");
                    }
                }
            }

            try
            {
                sURL = "UDP"; //reset URL as this part is only called every 30 min

                Task<string> response;
                if (string.IsNullOrEmpty(customerid) || customerid.Count(t => t == '.') == 3)
                    response = oClient.GetStringAsync(sURL + "/rest/v2/GetCatalog");
                else
                    response = oClient.GetStringAsync(sURL + "/rest/v2/GetCatalog?customerid=" + customerid);

                response.Wait(60000); //60s max

                if (response.IsCompleted)
                {
                    JavaScriptSerializer ser = new JavaScriptSerializer();
                    List<GetSoftware> lRes = ser.Deserialize<List<GetSoftware>>(response.Result);

                    if (lRes.Count > 500 && !customerid.StartsWith("--"))
                    {
                        try
                        {
                            File.WriteAllText(Path.Combine(Environment.ExpandEnvironmentVariables("%TEMP%"), "rzcat.json"), response.Result);
                        }
                        catch { }
                    }

                    return lRes;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("E2" + ex.Message, "GetCatalog");
                _sURL = ""; //enforce reload endpoint URL
            }

            return new List<GetSoftware>();
        }

        public static List<string> GetCategories(List<GetSoftware> oSWList)
        {
            List<string> lResult = new List<string>();

            foreach (GetSoftware oSW in oSWList)
            {
                lResult.AddRange((oSW.Categories ?? new List<string>()).ToArray());
            }

            return lResult.Distinct().OrderBy(t => t).ToList();
        }

        public static byte[] GetIcon(string iconhash, string customerid = "", int size = 0)
        {
            Task<Stream> response;

            response = oClient.GetStreamAsync(sURL + "/rest/v2/GetIcon?size=" + size + "&iconhash=" + iconhash);

            response.Wait(10000);

            if (response.IsCompleted)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    response.Result.CopyTo(ms);
                    byte[] bRes = ms.ToArray();
                    return bRes;
                }
            }

            return null;
        }

        public static List<AddSoftware> GetSoftwares(string productName, string productVersion, string manufacturer, string customerid = "")
        {
            try
            {
                Task<string> response;

                if (string.IsNullOrEmpty(customerid))
                    response = oClient.GetStringAsync(sURL + "/rest/v2/GetSoftwares?name=" + WebUtility.UrlEncode(productName) + "&ver=" + WebUtility.UrlEncode(productVersion) + "&man=" + WebUtility.UrlEncode(manufacturer));
                else
                    response = oClient.GetStringAsync(sURL + "/rest/v2/GetSoftwares?name=" + WebUtility.UrlEncode(productName) + "&ver=" + WebUtility.UrlEncode(productVersion) + "&man=" + WebUtility.UrlEncode(manufacturer) + "&customerid=" + WebUtility.UrlEncode(customerid));

                response.Wait(20000);
                if (response.IsCompleted)
                {
                    JavaScriptSerializer ser = new JavaScriptSerializer();
                    List<AddSoftware> lRes = ser.Deserialize<List<AddSoftware>>(response.Result);
                    return lRes;
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine("E1" + ex.Message, "GetSoftwares");
                _sURL = ""; //enforce reload endpoint URL
            }

            return new List<AddSoftware>();

        }

        public static string GetURL(string customerid)
        {
            using (HttpClient hClient = new HttpClient())
            {
                try
                {
                    Task<string> tReq;

                    if (string.IsNullOrEmpty(CustomerID))
                    {
                        using (HttpClient qClient = new HttpClient())
                        {
                            CustomerID = hClient.GetStringAsync("https://ruckzuck.tools/rest/v2/getip").Result;
                            customerid = CustomerID.ToString();
                        }
                    }


                    if (string.IsNullOrEmpty(customerid))
                    {
                        tReq = hClient.GetStringAsync("https://ruckzuck.tools/rest/v2/geturl");
                    }
                    else
                        tReq = hClient.GetStringAsync("https://ruckzuck.tools/rest/v2/geturl?customerid=" + customerid);



                    tReq.Wait(5000); //wait max 5s

                    if (tReq.IsCompleted)
                    {
                        _sURL = tReq.Result;
                        return _sURL;
                    }
                    else
                    {
                        _sURL = "https://ruckzuck.azurewebsites.net";
                        return _sURL;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("ERROR 145: " + ex.Message);
                }

                return "https://ruckzuck.azurewebsites.net";
            }
        }
        public static async void IncCounter(string shortname = "", string counter = "DL", string customerid = "")
        {
            try
            {
                await oClient.GetStringAsync(sURL + "/rest/v2/IncCounter?shortname=" + WebUtility.UrlEncode(shortname) + "&customerid=" + WebUtility.UrlEncode(CustomerID));
            }
            catch { }
        }
        public static bool UploadSWEntry(AddSoftware lSoftware, string customerid = "")
        {
            try
            {
                JavaScriptSerializer ser = new JavaScriptSerializer();
                HttpContent oCont = new StringContent(ser.Serialize(lSoftware), Encoding.UTF8, "application/json");

                var response = oClient.PostAsync(sURL + "/rest/v2/uploadswentry", oCont);
                response.Wait(30000); //30s max

                if (response.Result.StatusCode == HttpStatusCode.OK)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch { }

            return false;
        }
    }
}
