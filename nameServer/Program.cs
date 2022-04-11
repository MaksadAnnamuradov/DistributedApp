using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

class NameServer
{
    static Stopwatch sw = Stopwatch.StartNew();
    static UTF8Encoding encoding = new UTF8Encoding();
    static List<IPEndPoint> workersList = new List<IPEndPoint>();
    static void Main()
    {
        int dataNodePort = 6566;
        int distributorPort = 6544;

        //add server to distributor
        UdpClient nsClient = new UdpClient();
        var sendData = encoding.GetBytes("nameServer-add");
        nsClient.Send(sendData, sendData.Length, "127.0.0.1", distributorPort);

        var dbTask = Task.Run(() =>
        {
            UdpClient dbReceiver = new UdpClient(dataNodePort);

            while (true)
            {
                IPEndPoint newDbWorker = new IPEndPoint(0, 0);
                byte[] requestData = dbReceiver.Receive(ref newDbWorker);
                string requestString = encoding.GetString(requestData);


                if (requestString.Contains("free"))
                {
                    Console.WriteLine("Received {0} from worker {1}", requestString, newDbWorker);
                    Console.WriteLine("Adding worker {0} to queue", newDbWorker);
                    workersList.Add(newDbWorker);
                }
            }
        });

        var distributorTask = Task.Run(() =>
        {

            while (true)
            {
                IPEndPoint distRequester = new IPEndPoint(0, 0);
                byte[] requestData = nsClient.Receive(ref distRequester);
                string requestString = encoding.GetString(requestData);

                Console.WriteLine("Received {0} from distributor", requestString);

                Task.Run(() =>
                {
                    var random = new Random();
                    var workerNumber = random.Next(workersList.Count);

                    if (requestString.Contains(":"))
                    {
                        var name = requestString.Split(":")[0].Trim();
                        workerNumber = name.GetHashCode() % workersList.Count;
                    }
                    else
                    {
                        workerNumber = requestString.GetHashCode() % workersList.Count;
                    }

                    UdpClient jobClient = new UdpClient();

                    // Console.WriteLine($"Free workers: {workersList.Count}");

                    Console.WriteLine("Sending {0} to worker {1}", requestString, workersList.IndexOf(workersList[workerNumber]));

                    jobClient.Send(requestData, requestData.Length, workersList[workerNumber]);

                    IPEndPoint responder = new IPEndPoint(0, 0);

                    //wait for a message from the worker
                    byte[] workerResponse = jobClient.Receive(ref responder);
                    string workerResponseString = encoding.GetString(workerResponse);

                    Console.WriteLine("{0} received from worker {1}", workerResponseString, workerNumber);

                    Console.WriteLine("Sending {0} to distributor {1}", workerResponseString, distRequester);
                    byte[] responseData = encoding.GetBytes(workerResponseString);

                    nsClient.Send(responseData, responseData.Length, distRequester);

                });

            }

        });

        Task.WaitAll(distributorTask, dbTask);

    }



}