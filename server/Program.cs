using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

class Server
{
    static Stopwatch sw = Stopwatch.StartNew();
    static UTF8Encoding encoding = new UTF8Encoding();
    static readonly ConcurrentDictionary<long, List<long>> CacheDict =
            new ConcurrentDictionary<long, List<long>>();
    static Dictionary<IPEndPoint, string> workersList = new Dictionary<IPEndPoint, string>();
    static void Main()
    {
        int clientPort = 6544;

        UdpClient clientListener = new UdpClient();    //Create a UDPClient object
        // {
        //     clientListener.ExclusiveAddressUse = false; // Allow multiple clients to connect to the same socket/port
        // };
        clientListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true); // Connect even if socket/port is in use
        clientListener.Client.Bind(new IPEndPoint(IPAddress.Any, clientPort));


        int workerPort = 6555;

        UdpClient workerListener = new UdpClient();    //Create a UDPClient object
        // {
        //     workerListener.ExclusiveAddressUse = false; // Allow multiple clients to connect to the same socket/port
        // };
        workerListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true); // Connect even if socket/port is in use

        workerListener.Client.Bind(new IPEndPoint(IPAddress.Any, workerPort));


        int workerResponsePort = 6566;

        UdpClient workerResponseListener = new UdpClient();    //Create a UDPClient object
        workerResponseListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true); // Connect even if socket/port is in use
        workerResponseListener.Client.Bind(new IPEndPoint(IPAddress.Any, clientPort));



        while (true)
        {
            //start a thread to respond
            var clientTask = Task.Run(() =>
            {
                while(true){

                    IPEndPoint clientRequester = new IPEndPoint(0, 0);
                    byte[] requestData = clientListener.Receive(ref clientRequester);
                    string requestString = encoding.GetString(requestData);


                    // IPEndPoint workerRequester = new IPEndPoint(0, 0);
                    // byte[] workerRequesterData = workerResponseListener.Receive(ref workerRequester);

                    //long requestNum = long.Parse(requestString);

                    Console.WriteLine("Received {0} from {1}", requestString, clientRequester);


                    List<long> answer = new List<long>();

                    if (CacheDict.ContainsKey(requestString.GetHashCode()))
                    {
                        Console.WriteLine($"Number was already in cache for {requestString}");
                        answer = CacheDict.GetOrAdd(requestString.GetHashCode(), answer);
                    }
                    else //was not added
                    {
                        var freeWorkers = workersList.Where(x => x.Value == "free").ToList();
                        Console.WriteLine($"Free workers: {freeWorkers.Count}");


                        Console.WriteLine("Sending {0} to worker {1}", requestString, freeWorkers[0].Key);

                        byte[] workerData = encoding.GetBytes(requestString);
                        UdpClient toWorker = new UdpClient();
                        toWorker.Send(workerData, workerData.Length, freeWorkers[0].Key);


                        //wait for a message from the worker
                        IPEndPoint workerRequester = freeWorkers[0].Key;
                        //IPEndPoint workerResponseRequester = new IPEndPoint(0, 0);
                        byte[] recvBuffer = workerResponseListener.Receive(ref workerRequester);
                        string message = encoding.GetString(recvBuffer);
                        Console.WriteLine("{0} received from worker {1}", message, workerRequester);

                        answer = message.Split(',').Select(long.Parse).ToList();
                        CacheDict.GetOrAdd(requestString.GetHashCode(), answer);
                    }

                    Console.WriteLine("Starting task at time " + sw.Elapsed);

                    string response = String.Join(',', answer.Select(p => p.ToString()));

                    Console.WriteLine("Sending {0} to client {1}", response, clientRequester);
                    byte[] responseData = encoding.GetBytes(response);

                    UdpClient toClient = new UdpClient();

                    toClient.Send(responseData, responseData.Length, clientRequester);
                }
            });


             // start a thread to respond
            var workerTask = Task.Run(() =>
            {

                while(true){
                    

                    IPEndPoint requester = new IPEndPoint(0, 0);
                    byte[] requestData = workerListener.Receive(ref requester);
                    string requestString = encoding.GetString(requestData);

                    if(requestString.Contains("free"))
                    {
                        Console.WriteLine("Received {0} from worker {1}", requestString, requester);
                        workersList.Add(requester, "free");
                    }
                    else
                    {
                        Console.WriteLine("Received {0} from worker {1}", requestString, requester);
                        workersList.Add(requester, "busy");
                    }
                }
            });

            workerTask.Wait();
            clientTask.Wait();
        }
    }

   

}