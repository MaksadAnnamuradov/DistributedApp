using System.Net;
using System.Net.Sockets;
using System.Text;

class Server
{
    static void Main()
    {
        int requestsPort = 6544;
        UdpClient requestsClient = new UdpClient(requestsPort);
        UTF8Encoding encoding = new UTF8Encoding();

        while (true)
        {
            IPEndPoint requester = new IPEndPoint(0, 0);
            byte[] requestData = requestsClient.Receive(ref requester);
            // start a thread to respond
            Task.Run(() =>
            {
                string requestString = encoding.GetString(requestData);
                long request = long.Parse(requestString);
                List<long> answer = GetPrimeFactors(request);
                string response = String.Join(',', answer.Select(p => p.ToString()));
                byte[] responseData = encoding.GetBytes(response);
                UdpClient toClient = new UdpClient();
                Console.WriteLine("Sending response to {0} and response is {1}", requester.Address, Encoding.UTF8.GetString(responseData));
                toClient.Send(responseData, responseData.Length, requester);
            });
        }
    }

    static List<long> GetPrimeFactors(long product)
    {
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
        return factors;
    }

}