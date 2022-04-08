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
    static List<IPEndPoint> workersList = new List<IPEndPoint>();
    static void Main()
    {
        int serverPort = 6544;

        int workerPort = 6555;

        //Create a UDPClient object
        //serverReceiver.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true); // Connect even if socket/port is in use
        //serverReceiver.Client.Bind(new IPEndPoint(IPAddress.Any, serverPort));

        var workerTask = Task.Run(() =>
        {
            UdpClient workerReceiver = new UdpClient(workerPort);

            while (true)
            {

                IPEndPoint newWorker = new IPEndPoint(0, 0);
                byte[] requestData = workerReceiver.Receive(ref newWorker);
                string requestString = encoding.GetString(requestData);


                if (requestString.Contains("free"))
                {
                    Console.WriteLine("Received {0} from worker {1}", requestString, newWorker);
                    Console.WriteLine("Adding worker {0} to queue", newWorker);
                    workersList.Add(newWorker);
                }
            }
        });

        var clientTask = Task.Run(() =>
        {
            UdpClient clientReceiver = new UdpClient(serverPort);

            while (true)
            {

                IPEndPoint newClient = new IPEndPoint(0, 0);
                byte[] requestData = clientReceiver.Receive(ref newClient);
                string requestString = encoding.GetString(requestData);

                Task.Run(() =>
                {

                    UdpClient jobClient = new UdpClient();
                    List<long> answer = new List<long>();


                    if (CacheDict.ContainsKey(requestString.GetHashCode()))
                    {
                        Console.WriteLine($"Number was already in cache for {requestString}");
                        answer = CacheDict.GetOrAdd(requestString.GetHashCode(), answer);

                        string response = String.Join(',', answer.Select(p => p.ToString()));

                        Console.WriteLine("Sending {0} to client {1}", response, newClient);
                        byte[] responseData = encoding.GetBytes(response);

                        jobClient.Send(responseData, responseData.Length, newClient);

                    }
                    else //was not added
                    {
                        var freeWorker = workersList[0];
                        Console.WriteLine($"Free workers: {workersList.Count}");

                        Console.WriteLine("Sending {0} to worker {1}", requestString, freeWorker);

                        jobClient.Send(requestData, requestData.Length, freeWorker);

                        IPEndPoint responder = new IPEndPoint(0, 0);

                        //wait for a message from the worker
                        byte[] workerResponse = jobClient.Receive(ref responder);
                        string workerResponseString = encoding.GetString(workerResponse);

                        Console.WriteLine("{0} received from worker {1}", workerResponseString, freeWorker);

                        answer = workerResponseString.Split(',').Select(long.Parse).ToList();

                        CacheDict.GetOrAdd(requestString.GetHashCode(), answer);

                        string response = String.Join(',', answer.Select(p => p.ToString()));

                        Console.WriteLine("Sending {0} to client {1}", response, newClient);
                        byte[] responseData = encoding.GetBytes(response);

                        clientReceiver.Send(responseData, responseData.Length, newClient);
                    }
                });


            }

            
        });

        Task.WaitAll(clientTask, workerTask);

    }



}