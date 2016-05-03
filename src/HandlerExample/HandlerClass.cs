using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net; // For generating HTTP requests and getting responses.
using NiceHashBotLib; // Include this for Order class, which contains stats for our order.
using Newtonsoft.Json; // For JSON parsing of remote APIs.


	public class CoinInfo
	{
		public Int64 blocks { get; set; }
		public double price { get; set;}
		public int currentblocksize { get; set; }
		public int currentblocktx { get; set; }
		public double difficulty { get; set; }
		public string errors { get; set; }
		public int genproclimit { get; set; }
		public Int64 networkhashps { get; set; }
		public int pooledtx { get; set; }
		public bool testnet { get; set; }
		public string chain { get; set; }
		public bool generate { get; set; }
		public Int64 hashespersec { get; set; }	
	}

	public class Ticker{
		public double buy{ get; set;}
		public double high{ get; set;}
		public double last{ get; set;}
		public double low{ get; set;}
		public double sell{ get; set;}
		public double vol{ get; set;}
	}

	public class BTCInfo{
		//{"date":"1462265434","ticker":{"buy":"2891.14","high":"2899.14","last":"2891.14","low":"2852.7","sell":"2891.25","vol":"893315.464"}}
		public Int64 data{get; set;}
		public Ticker ticker{ get; set; }
	}


public class HandlerClass
{
    /// <summary>
    /// This method is called every 0.5 seconds.
    /// </summary>
    /// <param name="OrderStats">Order stats - do not change any properties or call any methods. This is provided only as read-only object.</param>
    /// <param name="MaxPrice">Current maximal price. Change this, if you would like to change the price.</param>
    /// <param name="NewLimit">Current speed limit. Change this, if you would like to change the limit.</param>
    public static void HandleOrder(ref Order OrderStats, ref double MaxPrice, ref double NewLimit)
    {
        // Following line of code makes the rest of the code to run only once per minute.
        if ((++Tick % 120) != 0) return;

        // Perform check, if order has been created at all. If not, stop executing the code.
        if (OrderStats == null) return;

        // Retreive JSON data from API server. Replace URL with your own API request URL.
		string jsonstring=GetHTTPResponseInJSON("http://220.178.235.84:5180/kpc"); 
		CoinInfo ci = (CoinInfo)Newtonsoft.Json.JsonConvert.DeserializeObject<CoinInfo> (jsonstring);
		Console.WriteLine ("开始计算..");
		double nethashG = ci.networkhashps / 1000000000.0;
		double hashpercent = 1 / nethashG;
		float poolfee = 0.1f;
		float saferadio = 0.93f;
		float nicehash_fee = 0.03f;
		double blockreward = 250 * (1 - poolfee);
		double btcprice = GetBTCPrice ();
		double coinprice = ci.price ; //btc price
		double coinpriceBTC=coinprice / btcprice;
		int blocktime = 60; //seconds
		double coinperday = blockreward * 3600 * 24 / blocktime;
		double coinperdayperG = coinperday * hashpercent;
		double btcperdayperG = coinperdayperG * coinpriceBTC;
		btcperdayperG = btcperdayperG * (1 - nicehash_fee) * saferadio;
		double rmbperdayperG = btcperdayperG * btcprice;
		Console.WriteLine ("saferadio:"+saferadio.ToString("00%"));
		Console.WriteLine ("nethash:"+nethashG.ToString("F2")+"GH");
		Console.WriteLine ("coinprice:"+coinprice.ToString("F4")+" RMB");
		Console.WriteLine ("btcprice:"+btcprice.ToString("F4")+" RMB [FROM OKCOIN.CN]");
		Console.WriteLine ("coinpriceBTC:"+coinpriceBTC.ToString("F8")+" BTC");
		Console.WriteLine ("blockreward:"+blockreward.ToString("F4") +" KPC");
		Console.WriteLine ("coinperday:"+coinperday.ToString("F4") +" KPC");
		Console.WriteLine ("coinperdayperG:"+coinperdayperG.ToString("F4") +" KPC");
		Console.WriteLine ("btcperdayperG:"+btcperdayperG.ToString("F8") +" BTC");
		Console.WriteLine ("rmbperdayperG:"+rmbperdayperG.ToString("F4") +" RMB");

        // Subtract service fees.
		btcperdayperG -= 0.04 * btcperdayperG;

        // Subtract minimal % profit we want to get.
		btcperdayperG -= 0.01 * btcperdayperG;

        // Set new maximal price.
		MaxPrice = Math.Floor(btcperdayperG * 10000) / 10000;

        // Example how to print some data on console...
        Console.WriteLine("Adjusting order #" + OrderStats.ID.ToString() + " maximal price to: " + MaxPrice.ToString("F4"));
    }


		private static double GetBTCPrice(){
			string jsondata = GetHTTPResponseInJSON ("https://www.okcoin.cn/api/v1/ticker.do?symbol=btc_cny");
			BTCInfo btcinfo=(BTCInfo)Newtonsoft.Json.JsonConvert.DeserializeObject<BTCInfo> (jsondata);
			return btcinfo.ticker.buy;
		}

    /// <summary>
    /// Data structure used for serializing JSON response from CoinWarz. 
    /// It allows us to parse JSON with one line of code and easily access every data contained in JSON message.
    /// </summary>
    #pragma warning disable 0649

    #pragma warning restore 0649


    /// <summary>
    /// Property used for measuring time.
    /// </summary>
    private static int Tick = -10;


    // Following methods do not need to be altered.
    #region PRIVATE_METHODS

    /// <summary>
    /// Get HTTP JSON response for provided URL.
    /// </summary>
    /// <param name="URL">URL.</param>
    /// <returns>JSON data returned by webserver or null if error occured.</returns>
    private static string GetHTTPResponseInJSON(string URL)
    {
        try
        {
            HttpWebRequest WReq = (HttpWebRequest)WebRequest.Create(URL);
            WReq.Timeout = 60000;
            WebResponse WResp = WReq.GetResponse();
            Stream DataStream = WResp.GetResponseStream();
            DataStream.ReadTimeout = 60000;
            StreamReader SReader = new StreamReader(DataStream);
            string ResponseData = SReader.ReadToEnd();
            if (ResponseData[0] != '{')
                throw new Exception("Not JSON data.");

            return ResponseData;
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    #endregion
}
