## Its.Log

[![Build Status](https://ci.appveyor.com/api/projects/status/github/jonsequitur/its.log?svg=true)](https://ci.appveyor.com/project/jonsequitur/its.log)

### Code instrumentation

Its.Log helps you instrument your code. It doesn't know anything about logging libraries, performance counters, or tracing, but it can be used to send a lot of information to all of them easily.

Log any object:


```csharp
Log.Write(() => ren);
```

...or several at once:

```csharp
Log.Write(() => new { ren, stimpy } );
          // writes this to the log:
          // SampleApp.SomeClass.Feed,Inputs: { ren = { FirstName = Ren, LastName = Hoek, Species = chihuahua, Weight = 1 } |
          // stimpy = { FirstName = Stimpy, LastName = , Species = cat, Weight = 5 } }
```

Send the output anywhere. Here's how you send it to the console:

```csharp
Log.EntryPosted += (sender, e) => Console.WriteLine(e.LogEntry.ToString());
```

Log boundaries:

```csharp
using (Log.Enter(() => new { ren, stimpy } ))
       // writes: Start: SampleApp.SomeClass.Feed,Inputs: { ren = { FirstName = Ren, LastName = Hoek, Species = chihuahua, Weight = 1 } |
       // stimpy = { FirstName = Stimpy, LastName = , Species = cat, Weight = 5 } }
{
     ren.Starve();
     stimpy.Feed();  

} // writes:
  // Stop:  SampleApp.SomeClass.Feed,Inputs: { ren = { FirstName = Ren, LastName = Hoek, Species = chihuahua, Weight = 0.5 } |
  // stimpy = { FirstName = Stimpy, LastName = , Species = cat, Weight = 8 } },Elapsed Ms: 3412,
```

Format anything with the ToLogString() extension method:

```csharp
Console.WriteLine(episodes.ToLogString());
// if episodes is null, it writes: [null]
// otherwise it writes using ToString: { SampleApp.Episode,  SampleApp.Episode,  SampleApp.Episode, (...12 more) }
```

Tell it to be more informative:

```csharp
Log.Formatters.RegisterPropertiesFormatter<Episode>();
Console.WriteLine(episodes.ToLogString());  
// now writes: { { Id = 4234, Title = Space Madness, Characters = { SampleApp.Character, SampleApp.Character } } }, (...12 more) }
```

Customize the output:

```csharp
Formatter<Episode>.RegisterForAllMembers(e => string.Format("Episode #{0}, Title: {1}", e.Number, e.Title));
```

Use The Reactive Extensions to query the log event stream...

```csharp
var errors = Log.Events().Where(e => e.Subject is Exception);
```

...or throttle them...

```csharp
var countOf404s = logEvents
                    .Where(e => e.SubjectIs<HttpException>())
                    .Where(e => e.GetSubject<HttpException>().GetHttpCode() == 404)
                    .Buffer(TimeSpan.FromSeconds(1))
                    .Select(es => es.Count());
```

