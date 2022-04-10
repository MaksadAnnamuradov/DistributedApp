using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

class Distributor
{
    static Stopwatch sw = Stopwatch.StartNew();
    static UTF8Encoding encoding = new UTF8Encoding();
    static readonly ConcurrentDictionary<long, List<long>> CacheDict =
            new ConcurrentDictionary<long, List<long>>();
    static Dictionary<string, IPEndPoint> serversDict = new Dictionary<string, IPEndPoint>();
    static void Main()
    {
        int clientsPort = 6533;

        int serversPort = 6544;

        var serverTask = Task.Run(() =>
        {
            UdpClient serverReceiver = new UdpClient(serversPort);

            while (true)
            {

                IPEndPoint newServerEndpoint = new IPEndPoint(0, 0);
                byte[] requestData = serverReceiver.Receive(ref newServerEndpoint);
                string requestString = encoding.GetString(requestData);


                if (requestString.Contains("add"))
                {
                    Console.WriteLine("Received {0} from server {1}", requestString, newServerEndpoint);

                    var serverName = requestString.Substring(0, requestString.IndexOf("-")).Trim();
                    Console.WriteLine("Adding server {0} to dict", serverName);
                    serversDict.Add(serverName, newServerEndpoint);
                }
            }
        });

        var clientTask = Task.Run(() =>
        {
            UdpClient clientReceiver = new UdpClient(clientsPort);

            while (true)
            {
                IPEndPoint clientRequester = new IPEndPoint(0, 0);
                byte[] requestData = clientReceiver.Receive(ref clientRequester);
                string requestString = encoding.GetString(requestData);

                Console.WriteLine("Received {0} from client {1}", requestString, clientRequester);

                Task.Run(() =>
                {
                    var serverName = requestString.Split("-")[0].Trim();
                    var requestArg = requestString.Split("-")[1].Trim();

                    //Console.WriteLine("Argument is {0}", requestArg);

                    UdpClient jobClient = new UdpClient();
                    List<long> answer = new List<long>();

                    if(serversDict.ContainsKey(serverName)){
                        Console.WriteLine("Sending {0} to server {1}", requestArg, serverName);

                        jobClient.Send(encoding.GetBytes(requestArg), requestArg.Length, serversDict[serverName]);

                        IPEndPoint responder = new IPEndPoint(0, 0);

                        //wait for a message from the server
                        byte[] serverResponse = jobClient.Receive(ref responder);
                        string serverResponseString = encoding.GetString(serverResponse);

                        Console.WriteLine("{0} received from server {1}", serverResponseString, serverName);

                        answer = serverResponseString.Split(',').Select(long.Parse).ToList();

                        string response = String.Join(',', answer.Select(p => p.ToString()));

                        Console.WriteLine("Sending {0} to client {1}", response, clientRequester);
                        byte[] responseData = encoding.GetBytes(response);

                        clientReceiver.Send(responseData, responseData.Length, clientRequester);

                    }
                });
            }
        });

        Task.WaitAll(clientTask, serverTask);

    }
}