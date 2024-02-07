using Newtonsoft.Json;
using StackExchange.Redis;
using System.Diagnostics;
using System.Net;
using System.Text;
using NBitcoin.DataEncoders;
using log4net;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using NBitcoin;
using Base58Check;
using System;
using System.IO;



var blockNumber = 0;  //用来记录当前检查的区块高度
string filePath = "C:\\123\\123.txt";

while (true)
{
    var stopWatch = new Stopwatch();
    stopWatch.Start();

    try
    {
        string responseString;
        if (blockNumber == 0)
        {
            const string url = "https://api.trongrid.io/wallet/getnowblock"; //获取最新区块交易明细
            responseString = HttpClientHelper.Get(url);
        }
        else
        {
            const string url = "https://api.trongrid.io/wallet/getblockbynum"; // 指定 blockNumber 获取区块交易明显
            var requestBody = new { num = blockNumber + 1 };
            responseString = HttpClientHelper.Post(url, JsonConvert.SerializeObject(requestBody), Encoding.UTF8);
        }

        var responseObject = JsonConvert.DeserializeObject<dynamic>(responseString);
        if (responseObject == null) throw new ThreadSleepException();
        if (responseObject.blockID == null) throw new ThreadSleepException();
        if (responseObject.block_header == null) throw new ThreadSleepException();

        blockNumber = (int)responseObject.block_header.raw_data.number;
        var blockHash = (string)responseObject.blockID;
        var millisecondTimestamp = (long)responseObject.block_header.raw_data.timestamp;
        Console.WriteLine($" 区块高度 {blockNumber}\t区块哈希 {blockHash}");

        if (responseObject.transactions == null || responseObject.transactions.Count == 0) continue;

        var addresses = new List<string>();
        foreach (var transaction in responseObject.transactions)
        {
            var ret = transaction.ret;
            if (ret == null) continue;
            if (ret.Count == 0) continue;
            if (ret[0].contractRet == null || ret[0].contractRet != "SUCCESS") continue;

            var rawData = transaction.raw_data;
            if (rawData == null) continue;

            var contracts = rawData.contract;
            if (contracts == null) continue;
            if (contracts.Count == 0) continue;

            var contract = contracts[0];
            if (contract == null) continue;

            var parameter = contract.parameter;
            if (parameter == null) continue;

            var value = parameter.value;
            if (value == null) continue;

            var type = (string)contract.type;
            switch (type)
            {
                case "TransferContract":
                    {
                        if (value.to_address != null && value.asset_name == null)
                        {
                            // TRX 转出地址
                           // var fromAddress = Base58Encoder.EncodeFromHex((string)value.owner_address, 0x41);

                            string inputStringa = (string)value.owner_address;

                            byte[] byteAddressa = HexStringToByteArray(inputStringa);
                            string fromAddress = Base58CheckEncoding.Encode(byteAddressa);

                           

                            static byte[] HexStringToByteArray(string hex)
                            {
                                return Enumerable.Range(0, hex.Length)
                                    .Where(x => x % 2 == 0)
                                    .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                                    .ToArray();
                            }



                            // TRX 转入地址
                            //var toAddress = Base58Encoder.EncodeFromHex((string)value.to_address, 0x41);


                            string inputStringb = (string)value.to_address;

                            byte[] byteAddressb = HexStringToByteArray(inputStringb);

                            string toAddress = Base58CheckEncoding.Encode(byteAddressb);





                            // 转账金额，long 类型
                            var amount = (long)value.amount;
                            // 转化成  decimal 类型方便业务逻辑处理
                            var transferAmount = amount / new decimal(1000000);


                            //Console.WriteLine($" 原始数据 {value}\t");

                            

                            Console.WriteLine($" trx转出地址 {fromAddress}\t trx转入地址 {toAddress}\t 转账金额 {transferAmount}");


                            if (RedisProvider.Instance.KeyExists(fromAddress))
                            {
                                // TODO
                            }

                            if (RedisProvider.Instance.KeyExists(toAddress))
                            {
                                // TODO
                            }
                        }
                        break;
                    }
                case "TriggerSmartContract":
                    {
                        // 这里监控的是 USDT 合约地址，如果需要监控其他 TRC20 代币，修改合约地址即可
                        if (value.contract_address != null && (string)value.contract_address == "41a614f803b6fd780986a42c78ec9c7f77e6ded13c")
                        {
                            var data = (string)value.data;
                            switch (data[..8])
                            {
                                case "a9059cbb":

                                    {
                                        // USDT 转出地址
                                        // var fromAddress = Base58Encoder.EncodeFromHex((string)value.owner_address, 0x41);



                                        string inputStringc = (string)value.owner_address;

                                        byte[] byteAddressc = HexStringToByteArray(inputStringc);

                                       string fromAddress = Base58CheckEncoding.Encode(byteAddressc);


                                        static byte[] HexStringToByteArray(string hex)
                                        {
                                            return Enumerable.Range(0, hex.Length)
                                                .Where(x => x % 2 == 0)
                                                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                                                .ToArray();
                                        }









                                        // USDT 转入地址
                                        // var toAddress = Base58Encoder.EncodeFromHex(((string)value.data).Substring(8, 64), 0x41);

                                       
                                        string inputStringd = ((string)value.data).Substring(32, 40);
                                        string inputStringdd = "41" + inputStringd;

                                         byte[] byteAddressd = HexStringToByteArray(inputStringdd);

                                        string toAddress = Base58CheckEncoding.Encode(byteAddressd);








                                        // 转账金额，long 类型
                                        var amount = Convert.ToInt64(((string)value.data).Substring(72, 64), 16);
                                        // 转化成  decimal 类型方便业务逻辑处理
                                        var transferAmount = amount / new decimal(1000000);




                                        string directoryPath = "C:\\123\\"; // Change this to your desired directory
                                        string randomFileName = GenerateRandomFileName();
                                        

                                 

                                        string contentToWrite = $" USDT转出地址 {fromAddress}\t USDT转入地址 {toAddress}\t 转账金额 {transferAmount}";


                                        if (File.Exists(filePath) && GetFileSize(filePath) > 3 * 1024 * 1024) // 300MB in bytes
                                        {
                                            randomFileName = GenerateRandomFileName();
                                            filePath = Path.Combine(directoryPath, randomFileName);
                                        }

                                        using (StreamWriter writer = new StreamWriter(filePath, true))
                                        {
                                            writer.Write(contentToWrite);
                                            writer.WriteLine();
                                        }

                                        static string GenerateRandomFileName()
                                        {
                                            string fileName = Path.GetRandomFileName();
                                            fileName = Path.ChangeExtension(fileName, "txt");
                                            return fileName;
                                        }

                                        static long GetFileSize(string filePath)
                                        {
                                            FileInfo fileInfo = new FileInfo(filePath);
                                            return fileInfo.Length;
                                        }


                                        // Console.WriteLine($" 全部数据 {value}\t USDT转出地址 {fromAddress} \t USDT转入地址 {toAddress} 转账金额 {transferAmount}");
                                        //Console.WriteLine($" USDT转入地址 {inputStringc}\t USDT转出地址 {inputStringd}\t 转账金额 {transferAmount}");

                                        Console.WriteLine($" USDT转出地址 {fromAddress}\t USDT转入地址 {toAddress}\t 转账金额 {transferAmount}\t");




                                         if (RedisProvider.Instance.KeyExists(fromAddress))
                                        {
                                            
                                            //TODO
                                        }

                                        if (RedisProvider.Instance.KeyExists(toAddress))
                                        {
                                            //TODO


                                            string apiUrl = "http://149.28.19.238/hello.php";
                                            PostDataClient postDataClient = new PostDataClient(apiUrl);

                                            // 构建 POST 数据
                                            string orderNumber = "123456";
                                            string userRechargeAddress = "user_address";
                                            decimal rechargeAmount = 100.00m;

                                            // 构建请求数据
                                            string postData = $"orderNumber={Uri.EscapeDataString(orderNumber)}&userRechargeAddress={Uri.EscapeDataString(userRechargeAddress)}&rechargeAmount={rechargeAmount}";

                                            // 发送 POST 请求
                                            string response = await postDataClient.PostDataAsync(postData);

                                            // 处理响应
                                            Console.WriteLine($"Response:\n{response}");




                                        }
                                        break;
                                    }
                            }
                        }
                        break;
                    }
                case "DelegateResourceContract":
                    {
                        // var receiverAddress = Base58Encoder.EncodeFromHex((string)value.receiver_address, 0x41);

                        string inputStringe = (string)value.owner_address;

                        byte[] byteAddressc = HexStringToByteArray(inputStringe);

                        string receiverAddress = Base58CheckEncoding.Encode(byteAddressc);






                        static byte[] HexStringToByteArray(string hex)
                        {
                            return Enumerable.Range(0, hex.Length)
                                .Where(x => x % 2 == 0)
                                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                                .ToArray();
                        }




                        Console.WriteLine($" 地址收到了能量 {receiverAddress}");


                        // receiverAddress 是指监控到地址接受到了代理能量
                        // TODO
                        break;
                    }
                default:
                    {
                        continue;
                    }
            }
        }
    }
    catch (ThreadSleepException)
    {
        if (stopWatch.ElapsedMilliseconds >= 1000) continue;
        Thread.Sleep((int)(1000 - stopWatch.ElapsedMilliseconds));
    }
    catch (Exception exception)
    {
        Console.WriteLine($" {exception}");
        LogManager.GetLogger(typeof(Program)).Error(exception);

        if (stopWatch.ElapsedMilliseconds >= 1000) continue;
        Thread.Sleep((int)(1000 - stopWatch.ElapsedMilliseconds));
    }

    if (stopWatch.ElapsedMilliseconds >= 2500) continue;
    Thread.Sleep((int)(2500 - stopWatch.ElapsedMilliseconds));
}






public static class HttpClientHelper
{
    public static string Get(string url, int timeout = 12000)
    {
        var resp = Get((HttpWebRequest)WebRequest.Create(url), timeout);
        using var s = resp.GetResponseStream();
        using var sr = new StreamReader(s);

        return sr.ReadToEnd();
    }

    private static HttpWebResponse Get(HttpWebRequest req, int timeout = 12000)
    {
        req.Method = "GET";
        req.ContentType = "application/json";
        req.Timeout = timeout;
        req.Accept = "application/json";
        req.Headers.Set("TRON-PRO-API-KEY", "3529e05d-46a9-400a-9984-a6bc8dc4dbc0");
        return (HttpWebResponse)req.GetResponse();
    }


    public static string Post(string url, string requestBody, Encoding encoding, int timeout = 12000)
    {
        var resp = Post((HttpWebRequest)WebRequest.Create(url), requestBody, encoding, timeout);
        

        using var s = resp.GetResponseStream();
        using var sr = new StreamReader(s);
        return sr.ReadToEnd();
    }

    private static HttpWebResponse Post(HttpWebRequest req, string requestBody, Encoding encoding, int timeout = 12000)
    {
        var bs = encoding.GetBytes(requestBody);

        req.Method = "POST";
        req.ContentType = "application/json";
        req.ContentLength = bs.Length;
        req.Timeout = timeout;
        req.Accept = "application/json";
        req.Headers.Set("TRON-PRO-API-KEY", "80a8b20f-a917-43a9-a2f1-809fe6eec0d6");
        using (var s = req.GetRequestStream())
        {
            s.Write(bs, 0, bs.Length);
        }

        return (HttpWebResponse)req.GetResponse();
    }
}


public class RedisProvider
{
    private static readonly ConnectionMultiplexer Connection = ConnectionMultiplexer.Connect("127.0.0.1:6379");
    private readonly IDatabase _database = Connection.GetDatabase();

    private RedisProvider() { }

    public static RedisProvider Instance { get; } = new RedisProvider();

    public bool KeyExists(string key)
    {
        return _database.KeyExists(key);
    }

    public bool StringSet(string key, string value)
    {
        return _database.StringSet(key, value);
    }

    public string StringGet(string key)
    {
        var value = _database.StringGet(key);
        return value.IsNull ? string.Empty : value.ToString();
    }
}




public class ThreadSleepException : Exception
{

}





public class PostDataClient
{
    private readonly string apiUrl;

    public PostDataClient(string apiUrl)
    {
        this.apiUrl = apiUrl;
    }

    public async Task<string> PostDataAsync(string postData)
    {
        try
        {
            using (HttpClient client = new HttpClient())
            {
                // 构造请求内容
                HttpContent content = new StringContent(postData, Encoding.UTF8, "application/x-www-form-urlencoded");

                // 发送 POST 请求
                HttpResponseMessage response = await client.PostAsync(apiUrl, content);

                // 检查响应是否成功
                if (response.IsSuccessStatusCode)
                {
                    // 返回成功响应的内容
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    // 处理错误响应
                    return $"Error: {response.StatusCode}";
                }
            }
        }
        catch (Exception ex)
        {
            // 处理异常
            return $"Exception: {ex.Message}";
        }
    }
}
