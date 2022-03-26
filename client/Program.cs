using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Diagnostics;
using System;

namespace Client
{
    class Program
    {
        // int PORT = 6544;
        // UdpClient udpClient = new UdpClient();
        static Random rand = new Random();
        static int min = 3;
        static int max = 1000;
        static int numClients = 11;
        // static int counter = 1;

        static Stopwatch sw = Stopwatch.StartNew();

        // Probabilistically find a prime number within the range [min, max].
        static double NextPrime()
        {
            // Try random numbers until we find a prime.
            for ( ; ; )
            {
                int p = rand.Next(min, max + 1);
                if (p % 2 == 0) continue;

                // See if it's prime.
                if (CheckIfPrime(p)) return p;
            }
        }

        static bool CheckIfPrime(double n) //to check if the random number generated is prime
        {
            var sqrt = Math.Sqrt(n);
            for (var i = 2; i <= sqrt; i++){
                if (n % i == 0) return false;
            }
            return true;
        }

        
        static void setMinThreadPoolThreads(int count)
        {
            Console.WriteLine("\nSetting min thread pool threads to {0}.\n", count);
            int workerThreads, completionPortThreads;
            ThreadPool.GetMinThreads(out workerThreads, out completionPortThreads);
            ThreadPool.SetMinThreads(count, completionPortThreads);
        }


        static async Task runTasks(){
            var sw = Stopwatch.StartNew();
            Console.WriteLine("\nStarting tasks.");

             var tasks = new List<Task>();

            for (int i = 0; i < numClients; ++i)
                tasks.Add(Task.Run(new Action(clientTask)));

            await Task.WhenAll(tasks);
        }

        static void clientTask()
        {
            Console.WriteLine("Task starting at time " + sw.Elapsed);
            
            int PORT = 6544;
            UdpClient udpClient = new UdpClient();
            var data = NextPrime();
            // int data = rand.Next(min, max + 1);
            udpClient.Send(BitConverter.GetBytes(data), data.ToString().Length, "255.255.255.255", PORT);

            var from = new IPEndPoint(0, 0);
            byte[] recvBuffer = udpClient.Receive(ref from);
            string message = Encoding.UTF8.GetString(recvBuffer);
            Console.WriteLine("{0} received from {2}", message, from);
            Console.WriteLine("Task stopping at time " + sw.Elapsed);
        }
        static void Main()
        {
            setMinThreadPoolThreads(30);
            var tasks = runTasks();
            Console.WriteLine("Waiting for tasks to finish.");
            tasks.Wait();
            Console.WriteLine("Finished after " + sw.Elapsed);
        }
    }
}