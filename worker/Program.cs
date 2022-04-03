using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Diagnostics;
using System;


class Worker
{

    static UTF8Encoding encoding = new UTF8Encoding();
    static Stopwatch sw = Stopwatch.StartNew();

    // create a list of states
    static string workerState = "free";
    
    static void Main()
    {
        while (true)
        {

            

            int PORT = 6555;
            int workerResponsePort = 6566;

            UdpClient udpClient = new UdpClient();

            udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true); // Connect even if socket/port is in use

            udpClient.Client.Bind(new IPEndPoint(0, 0));

            //wait for a message from the server
            // var from = new IPEndPoint(0, 0);
            // byte[] recvBuffer = udpClient.Receive(ref from);
            // string message = encoding.GetString(recvBuffer);
            // Console.WriteLine("{0} received from {1}", message, from);
            
            var from = new IPEndPoint(0, 0);

        
            if(workerState == "free")
            {
                var sendData = encoding.GetBytes("free");
                Console.WriteLine("Sending {0} state to the server", workerState);
                udpClient.Send(sendData, sendData.Length, "255.255.255.255", PORT);

                //wait for a message from the server
                byte[] recvBuffer = udpClient.Receive(ref from);
                string message = encoding.GetString(recvBuffer);
                Console.WriteLine("{0} received from server {1}", message, from);


                long requestNum = long.Parse(message);
                workerState = "busy";

                
                //call getPrimeFactors
                var answer = GetPrimeFactors(requestNum);

                string response = String.Join(',', answer.Select(p => p.ToString()));

                Console.WriteLine("Sending {0} to server {1}", response, workerResponsePort);
                byte[] responseData = encoding.GetBytes(response);
                UdpClient toClient = new UdpClient();
                toClient.Send(responseData, responseData.Length, "225.255.255.255", workerResponsePort);

                workerState = "free";
            }
            // else{
            //     string response = workerState;

            //     Console.WriteLine("Sending {0} to {1}", response, from);
            //     byte[] responseData = encoding.GetBytes(response);

            //     UdpClient toClient = new UdpClient();
            //     toClient.Send(responseData, responseData.Length, from);
            // }
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
