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
    static void Main()
    {
        while (true)
        {
            // start a thread to respond
            var clientTask = Task.Run(() =>
            {

                int clientPort = 6544;

                UdpClient listener = new UdpClient();    //Create a UDPClient object
                {
                    listener.ExclusiveAddressUse = false; // Allow multiple clients to connect to the same socket/port
                };

                listener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true); // Connect even if socket/port is in use

                listener.Client.Bind(new IPEndPoint(IPAddress.Any, clientPort));

                while(true){

                    IPEndPoint requester = new IPEndPoint(0, 0);
                    byte[] requestData = listener.Receive(ref requester);
                    string requestString = encoding.GetString(requestData);

                    long requestNum = long.Parse(requestString);

                    Console.WriteLine("Received {0} from {1}", requestString, requester);


                    List<long> answer = new List<long>();

                    if (CacheDict.ContainsKey(requestString.GetHashCode()))
                    {
                        Console.WriteLine($"Number was already in cache for {requestNum}");
                        answer = CacheDict.GetOrAdd(requestString.GetHashCode(), answer);
                    }
                    else //was not added
                    {
                        Console.WriteLine($"Calling Prime factors and adding {requestNum} to cache");
                        answer = GetPrimeFactors(requestNum);
                        CacheDict.GetOrAdd(requestString.GetHashCode(), answer);
                    }

                    Console.WriteLine("Starting task at time " + sw.Elapsed);

                    string response = String.Join(',', answer.Select(p => p.ToString()));

                    Console.WriteLine("Sending {0} to {1}", response, requester);
                    byte[] responseData = encoding.GetBytes(response);

                    UdpClient toClient = new UdpClient();

                    toClient.Send(responseData, responseData.Length, requester);
                }
            });


             // start a thread to respond
            var workerTask = Task.Run(() =>
            {

                int workerPort = 6555;

                UdpClient listener = new UdpClient();    //Create a UDPClient object
                {
                    listener.ExclusiveAddressUse = false; // Allow multiple clients to connect to the same socket/port
                };
                listener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true); // Connect even if socket/port is in use

                listener.Client.Bind(new IPEndPoint(IPAddress.Any, workerPort));

                IPEndPoint requester = new IPEndPoint(0, 0);
                byte[] requestData = listener.Receive(ref requester);
                string requestString = encoding.GetString(requestData);

                long requestNum = long.Parse(requestString);

                Console.WriteLine("Received {0} from {1}", requestString, requester);


                List<long> answer = new List<long>();

                if (CacheDict.ContainsKey(requestString.GetHashCode()))
                {
                    Console.WriteLine($"Number was already in cache for {requestNum}");
                    answer = CacheDict.GetOrAdd(requestString.GetHashCode(), answer);
                }
                else //was not added
                {
                    Console.WriteLine($"Calling Prime factors and adding {requestNum} to cache");
                    answer = GetPrimeFactors(requestNum);
                    CacheDict.GetOrAdd(requestString.GetHashCode(), answer);
                }

                Console.WriteLine("Starting task at time " + sw.Elapsed);

                string response = String.Join(',', answer.Select(p => p.ToString()));

                Console.WriteLine("Sending {0} to {1}", response, requester);
                byte[] responseData = encoding.GetBytes(response);

                UdpClient toClient = new UdpClient();

                toClient.Send(responseData, responseData.Length, requester);
            });

            clientTask.Wait();
            workerTask.Wait();
        }
    }

    static List<long> GetPrimeFactors(long product)
    {
        var sw = Stopwatch.StartNew();
        Console.WriteLine("Getting Prime factors for {0}", product);

        if (product <= 1)
        {
            throw new ArgumentException();
        }
        List<long> factors = new List<long>();
        // divide out factors in increasing order until we get to 1
        while (product > 1)
        {
            for (long i = 2; i <= product; i++)
            {
                if (product % i == 0)
                {
                    product /= i;
                    factors.Add(i);
                    break;
                }
            }
        }
        Console.WriteLine("Prime factors finished after " + sw.Elapsed);
        return factors;
    }

}