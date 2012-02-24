![Logo](https://github.com/EastPoint/Burden/raw/master/logo-128.png)

# Burden

Burden leverages the power of the [Reactive Extensions](http://msdn.microsoft.com/en-us/data/gg577609) to provide a typed durable job queue.  At the moment, the only supported back-end storage for job input is Redis via the Burden.Redis library.

The basic concept is that job input messages are asynchronously relayed through an Rx collection to a back-end durable store.  At a later time they are asynchronously picked up and executed, at which point they are moved to either a completed queue or a poisoned queue.  The concurrency of executing jobs is managed by the Rx task scheduler (which generally relies on [Scheduler.TaskPool](http://msdn.microsoft.com/en-us/library/system.reactive.concurrency.scheduler.taskpool%28v=vs.103%29.aspx))

This type of design can be useful to queue up background syncs with external services for instance, but there are many other use cases where you want to kick off background jobs in a manner that has a reasonable amount of guarantee that the job will eventually complete if the original host process dies.

## Can't you already do this with Rx?

Yes, and no.  

You can create an in-proc / in-memory messaging system pretty easily with Rx using Observables, but that state will only live as long as the process does.  There is no baked in way to make the job inputs durable / reliable over time, nor is there an easy way that I'm aware of to track the completion / error state of jobs.

This library hopes to fill that gap.

## Installation

* Install-Package Burden
* Install-Package Burden.Redis

### Requirements

* .NET Framework 4+ Client Profile

### Usage Examples

Look at Burden.Tests project for a number of usage examples.

The most important class is ```MonitoredJobQueue```, which serves as the entry point to creating durable queues.

The basic usage from a code perspective is to create a job queue instance, associating it with a job that expects a given input type and generates a given output type.  As new jobs need to be executed, simply add inputs to the queue and they will later be processed by the specified Func<T>.

From an implementation standpoint, add job input to queue -> queue async writes input to durable store -> inputs are later async picked up -> jobs are executed, the input is removed from the queue and the output is written to a completed queue.  For failed jobs, the input and basic job exception details are written to a poison queue.

Each piece of the system is customizable to fit whatever specific needs are.


### Basic Queue Creation

In it's most basic usage, the static factory method ```MonitoredJobQueue.Create``` will accept a factory for creating durable job storage, a ```Func<TInput, TOutput>``` to define the job execution code and a maximum number of concurrent jobs to run.

The following example stores job inputs in Redis, checks Redis every 3 seconds for new inputs, and will execute up to 10 concurrent jobs.  This job does nothing more than write the job input to the console, and returns void - [System.Reactive.Unit](http://msdn.microsoft.com/en-us/library/system.reactive.unit%28v=vs.103%29.aspx)) in Rx parlance.

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

### Customizing the queue system 

## Similar Projects

* [Stact](https://github.com/phatboyg/Stact) - Distributed Actor Framework for .NET.  
* [Retlang](http://code.google.com/p/retlang/) - High performance in-memory messaging.

## Future Improvements

* Ensure Mono works properly
* No work has been done to consider a multiple consumer scenario where message de-duplication can be performed (whether this is an issue for you depends on the idempotency of the particular job)
* There are ways to filter incoming job inputs (for example, to prevent running idempotent jobs too many times in a short period of time), but there is presently no way to replicate this de-duplication when the completion status is written to the durable store.

## Contributing

Fork the code, and submit a pull request!  

Any useful changes are welcomed.  If you have an idea you'd like to see implemented that strays far from the simple spirit of the application, ping us first so that we're on the same page.

## Credits

* Inspiration is from [Rx Power Toys](http://rxpowertoys.codeplex.com/) that defines a similar in-memory job queue for anonymous actions.

* The logo is derived from an image on [IconsPedia](http://www.iconspedia.com/icon/rank-blue-icon-22265.html).  The image was created by [Gakuseisean](http://gakuseisean.deviantart.com/)