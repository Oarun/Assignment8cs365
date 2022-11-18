using System.Collections.Concurrent;
using System.Diagnostics;
public class Program
{
    static int numOfWorkers = 2;
    static int meanTimeBtwnCustomer = 10 * 10;
    static int meanServiceTime = 30 * 10;
    static int timeRun = 70 * 10;
    static int currentSimTime = 0;
    static int customerServed = 0;
    static double avrgTimeInLine = 0;
    static Semaphore workerSemaphore;
    static Mutex currentSimTimeMutex = new Mutex();
    static Mutex customerServedMutex = new Mutex();
    static Mutex avrgTimeInLineMutex = new Mutex();

    static BlockingCollection<Cart> carts = new BlockingCollection<Cart>();
    static void customer(object arg){
        Stopwatch stopWatch = new Stopwatch();
        Stopwatch waitWatch = new Stopwatch();
        stopWatch.Start();
        waitWatch.Start();
        Console.WriteLine($"At time {currentSimTime / 10}, customer {Thread.CurrentThread.ManagedThreadId - 4} arrives in line.");

        // Wait for a worker to become available
        workerSemaphore.WaitOne();
        waitWatch.Stop();
        TimeSpan wait = waitWatch.Elapsed;
        int doneTime = wait.Milliseconds;
        avrgTimeInLineMutex.WaitOne();
            avrgTimeInLine += doneTime;
        avrgTimeInLineMutex.ReleaseMutex();

        Console.WriteLine($"At time {currentSimTime / 10}, customer {Thread.CurrentThread.ManagedThreadId - 4} is being served.");
        Thread.Sleep(meanServiceTime);

        workerSemaphore.Release();
        stopWatch.Stop();
        TimeSpan ts = stopWatch.Elapsed;

        currentSimTimeMutex.WaitOne();
            currentSimTime += ts.Milliseconds;
        currentSimTimeMutex.ReleaseMutex();
       
        customerServedMutex.WaitOne();
            customerServed++;
        customerServedMutex.ReleaseMutex();


        Console.WriteLine($"At time {currentSimTime / 10}, customer {Thread.CurrentThread.ManagedThreadId - 4} leaves the food cart.");

    }
    static void QueueCallers(object? arg)
    {

        while(currentSimTime <= timeRun)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            Thread.Sleep(meanTimeBtwnCustomer);
            if(currentSimTime <= timeRun){
                carts.TryAdd(new Cart {CartThread = new Thread(new ParameterizedThreadStart(customer))});
            }

            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            currentSimTimeMutex.WaitOne();
                currentSimTime += ts.Milliseconds;
            currentSimTimeMutex.ReleaseMutex();
            
        }
    }
    public static void Main(string[] args){
        /*
        Console.WriteLine("How many workers?: ");
        numOfWorkers = Convert.ToInt32(Console.ReadLine());
        workerSemaphore = new Semaphore(numOfWorkers, numOfWorkers);

        Console.WriteLine("What is the mean time between customers?: ");
        meanTimeBtwnCustomer = Convert.ToInt32(Console.ReadLine());

        Console.WriteLine("What is the mean service time?: ");
        meanServiceTime = Convert.ToInt32(Console.ReadLine());

        Console.WriteLine("How long will simulation run for?: ");
        timeRun = Convert.ToInt32(Console.ReadLine());
        */

        workerSemaphore = new Semaphore(numOfWorkers, numOfWorkers);

        var queueThread = new Thread(new ParameterizedThreadStart(QueueCallers));
        queueThread.Start();
        Thread.Sleep(meanTimeBtwnCustomer + 500);
        
        while (carts.TryTake(out var cart))
        {
            cart?.CartThread?.Start(null);
        }

        while (carts.TryTake(out var cart))
        {
            cart?.CartThread?.Join();
            
        }
        Thread.Sleep(3000);
        Console.WriteLine($"Simulation terminated after {customerServed} customers served.");
        Console.WriteLine($"Average waiting time = {avrgTimeInLine/customerServed}");


 
    }
}