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
    static int SERVER_PORT = 6544;
    
    static void Main()
    {
        UdpClient workerClient = new UdpClient();


        var sendData = encoding.GetBytes("free");
        Console.WriteLine("Sending {0} state to the server", workerState);
        workerClient.Send(sendData, sendData.Length, new IPEndPoint(0, SERVER_PORT));

        while (true)
        {
            IPEndPoint requester = new IPEndPoint(0, 0);

            //wait for a message from the server
            byte[] recvBuffer = workerClient.Receive(ref requester);

            Task.Run(()=>{
                string message = encoding.GetString(recvBuffer);
                Console.WriteLine("{0} received from server {1}", message, requester);

                long requestNum = long.Parse(message);
                
                //call getPrimeFactors
                var answer = GetPrimeFactors(requestNum);

                string response = String.Join(',', answer.Select(p => p.ToString()));

                Console.WriteLine("Sending {0} to server {1}", response, requester);
                byte[] responseData = encoding.GetBytes(response);

                UdpClient toClient = new UdpClient();
                toClient.Send(sendData, sendData.Length, requester);

            });
       
          
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
