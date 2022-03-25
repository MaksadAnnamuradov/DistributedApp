using System.Net;
using System.Net.Sockets;
using System.Text;

class Server
{
    static void Main()
    {
        while (true)
        {
            string request = Console.ReadLine();
            if(!string.IsNullOrEmpty(request)){

                List<long> answer = GetPrimeFactors(long.Parse(request));
                string response = String.Join(',', answer.Select(p => p.ToString()));
                byte[] responseData = Encoding.UTF8.GetBytes(response);
                string message = Encoding.UTF8.GetString(responseData);
                Console.WriteLine(message);
            }
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