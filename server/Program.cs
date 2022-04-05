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
    static Queue<IPEndPoint> workersQueue = new Queue<IPEndPoint>();
    static void Main()
    {
        int serverPort = 6544;
        UdpClient serverReceiver = new UdpClient();    //Create a UDPClient object
        serverReceiver.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true); // Connect even if socket/port is in use
        serverReceiver.Client.Bind(new IPEndPoint(IPAddress.Any, serverPort));

        while (true)
        {

            IPEndPoint requester = new IPEndPoint(0, 0);
            byte[] requestData = serverReceiver.Receive(ref requester);
            string requestString = encoding.GetString(requestData);


            if(requestString.Contains("free"))
            {
                Console.WriteLine("Received {0} from worker {1}", requestString, requester);
                Task.Run(()=>{
                    Console.WriteLine("Adding worker {0} to queue", requester);
                    workersQueue.Enqueue(requester);
                });
            }
            else
            {
                UdpClient serverSender = new UdpClient();

                List<long> answer = new List<long>();
               
                Task.Run(()=>{
                    if (CacheDict.ContainsKey(requestString.GetHashCode()))
                    {
                        Console.WriteLine($"Number was already in cache for {requestString}");
                        answer = CacheDict.GetOrAdd(requestString.GetHashCode(), answer);

                        string response = String.Join(',', answer.Select(p => p.ToString()));

                        Console.WriteLine("Sending {0} to client {1}", response, requester);
                        byte[] responseData = encoding.GetBytes(response);

                        serverSender.Send(responseData, responseData.Length, requester);
           
                    }
                    else //was not added
                    {
                        var freeWorker = workersQueue.Dequeue();
                        Console.WriteLine($"Free workers: {workersQueue.Count}");

                        Console.WriteLine("Sending {0} to worker {1}", requestString, freeWorker);
                    
                        serverSender.Send(requestData, requestData.Length, freeWorker);

                        //wait for a message from the worker
                        byte[] workerResponse = serverReceiver.Receive(ref freeWorker);
                        string workerResponseString = encoding.GetString(workerResponse);

                        Console.WriteLine("{0} received from worker {1}", workerResponseString, freeWorker);

                        answer = workerResponseString.Split(',').Select(long.Parse).ToList();

                        CacheDict.GetOrAdd(requestString.GetHashCode(), answer);

                        string response = String.Join(',', answer.Select(p => p.ToString()));

                        Console.WriteLine("Sending {0} to client {1}", response, requester);
                        byte[] responseData = encoding.GetBytes(response);

                        serverSender.Send(responseData, responseData.Length, requester);
                    }
                });
            }
       
        }
    }

   

}