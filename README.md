![Logo](https://github.com/EastPoint/Burden/raw/master/logo-128.png)

# Burden

Burden leverages the power of the [Reactive Extensions](http://msdn.microsoft.com/en-us/data/gg577609) to provide a typed durable job queue.  At the moment, the only supported back-end storage for job inputs is Redis via the Burden.Redis library.

The basic concept is that job input messages are asynchronously relayed through an Rx collection to a back-end durable store.  At a later time they are asynchronously picked up and executed, at which point they are moved to either a completed queue or a poisoned queue.

## Installation

* Install-Package Burden
* Install-Package Burden.Redis

### Requirements

* .NET Framework 4+ Client Profile

### Usage Examples

Look at Burden.Tests project for a number of usage examples.

The most important class is ```MonitoredJobQueue```, which serves as the entry point to creating durable queues.

### Basic Queue Creation

In it's most basic usage, the static factory method ```MonitoredJobQueue.Create``` will accept a factory for creating durable job storage, a ```Func<TInput, TOutput>``` to define the job execution code and a maximum number of concurrent jobs to run.

The following example stores job inputs in Redis, polls Redis every 3 seconds for new inputs, and will execute up to 10 concurrent jobs.  This job does nothing more than write the job input to the console, and returns void (Unit in System.Reactive)

```csharp
  private static int counter;
  private static readonly int jobCount = 20;
  private RedisConnection connection = RedisHostManager.Current(visible: true);
  private static ManualResetEventSlim unlocked = new ManualResetEventSlim(false);

  private IRedisClientsManager GetClientManager()
  {
    return new BasicRedisClientManager(String.Format("{0}:{1}", connection.Host, connection.Port));
  }

  private Func<int, System.Reactive.Unit> Job = new Func<int, System.Reactive.Unit>(input =>
    {
      Console.WriteLine("Running job number [" + input + "]...");

      if (Interlocked.Increment(ref counter) == jobCount)
      {
        unlocked.Set();
      }
      
      return new System.Reactive.Unit();
    });

  [Fact]
  public void QueuesJobsAndExecutesThem()
  {
    var factory = new RedisJobQueueFactory(GetClientManager(), QueueNames.Default);
    using (var queue = MonitoredJobQueue.Create(factory, Job, maxConcurrentJobsToExecute: 10, pollingInterval: TimeSpan.FromSeconds(3)))
    {
      //load the queue with job inputs
      for (int i = 0; i < jobCount; i++)
      {
        queue.AddJob(i);
      }

      unlocked.Wait(TimeSpan.FromSeconds(5));
    }
    Assert.Equal(jobCount, counter);
  }
```

```TODO: More Examples - explanations of various pieces```

## Similar Projects

* [Stact](https://github.com/phatboyg/Stact) - Distributed Actor Framework for .NET.  
* [Retlang](http://code.google.com/p/retlang/) - High performance in-memory messaging.

## Future Improvements

* Ensure Mono works properly

## Contributing

Fork the code, and submit a pull request!  

Any useful changes are welcomed.  If you have an idea you'd like to see implemented that strays far from the simple spirit of the application, ping us first so that we're on the same page.

## Credits

* Inspiration is from [Rx Power Toys](http://rxpowertoys.codeplex.com/) that defines a similar in-memory job queue for anonymous actions.

* The logo is derived from an image on [IconsPedia](http://www.iconspedia.com/icon/rank-blue-icon-22265.html).  The image was created by [Gakuseisean](http://gakuseisean.deviantart.com/)