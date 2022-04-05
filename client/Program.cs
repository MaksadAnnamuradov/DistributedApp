using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Diagnostics;
using System;

namespace Client
{
    class Program
    {
        static Random rand = new Random();
        static long min = 1000000;
        static long max = 100000000;
        static int numClients = 10;
        static UTF8Encoding encoding = new UTF8Encoding();
        static Stopwatch sw = Stopwatch.StartNew();
        
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
            
            int SERVER_PORT = 6544;
            UdpClient udpClient = new UdpClient();
            // var data = NextPrime();
            long data = rand.NextInt64(min, max + 1);
            var sendData = encoding.GetBytes(data.ToString());
            Console.WriteLine("Sending data {0} to server {1}", data, SERVER_PORT);
            udpClient.Send(sendData, data.ToString().Length, "255.255.255.255", SERVER_PORT);

            var from = new IPEndPoint(0, 0);
            byte[] recvBuffer = udpClient.Receive(ref from);
            string message = encoding.GetString(recvBuffer);
            Console.WriteLine("{0} received from {1}", message, from);
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