using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

class Server
{
    static Stopwatch sw = Stopwatch.StartNew();
    static UTF8Encoding encoding = new UTF8Encoding();
    static void Main()
    {
        int requestsPort = 6544;
        // UdpClient requestsClient = new UdpClient(requestsPort);


        UdpClient listener = new UdpClient();    //Create a UDPClient object
        {
            listener.ExclusiveAddressUse = false; // Allow multiple clients to connect to the same socket/port
        };
        listener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true); // Connect even if socket/port is in use

        listener.Client.Bind(new IPEndPoint(IPAddress.Any, requestsPort));
        // IPEndPoint requester = new IPEndPoint(IPAddress.Any, requestsPort);   //Start receiving data from any IP listening on port 5606 (port for PCARS2)

        while (true)
        {
            IPEndPoint requester = new IPEndPoint(0, 0);
            byte[] requestData = listener.Receive(ref requester);
            Console.WriteLine("Received {0} bytes from {1}", requestData.Length, requester);

            // start a thread to respond
            Task.Run(() =>
            {
                Console.WriteLine("Starting task at time " + sw.Elapsed);
                string requestString = encoding.GetString(requestData);
                long request = long.Parse(requestString);
                List<long> answer = GetPrimeFactors(request);
                string response = String.Join(',', answer.Select(p => p.ToString()));
                Console.WriteLine("Sending {0} to {1}", response, requester);
                byte[] responseData = encoding.GetBytes(response);
                UdpClient toClient = new UdpClient();
                Console.WriteLine("Sending response to {0} and response is {1}", requester.Address, Encoding.UTF8.GetString(responseData));
                toClient.Send(responseData, responseData.Length, requester);
            });
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
        Console.WriteLine("Finished after " + sw.Elapsed);
        return factors;
    }

}