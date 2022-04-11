# DistributedApp

 Sample App to demonstarte distributed computing by threading in C#.

### Server

 Server takes in requests from multiple clients, and responds with PrimeFactors for the given number by creating a new thread for each request. If the recieved number exsist in the ConcurrentDictionary, it will return already computed prime factors for that number. If not, it will compute the prime factor and add the answer to dictionary so that it does not need to compute again.

### Client

 Creating multiple clients and assigning different ports for each client, and then running the request to server in new task. And then wait for all tasks to finish and display the returned value to the console.

### Simple Client

 Just takes in input number from the user and computes the prime factors without threading.

![Diagram](distributedDesign.drawio.svg)
