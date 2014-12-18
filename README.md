## Its.Log

[![Build Status](https://ci.appveyor.com/api/projects/status/github/jonsequitur/its.log?svg=true)](https://ci.appveyor.com/project/jonsequitur/its-log)

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
          // { Information: CallingType = Characters | CallingMethod = Dinner | Subject = { ren = { FirstName = Ren, LastName = Hoek, Species = chihuahua, Weight = 1 } | stimpy = { FirstName = Stimpy, LastName = [null], Species = cat, Weight = 5 } } | TimeStamp = 2014-12-18 16:11:00Z }
```

Send the output anywhere. Here's how you send it to the console:

```csharp
Log.EntryPosted += (sender, e) => Console.WriteLine(e.LogEntry.ToString());
```

Log boundaries:

```csharp
using (Log.Enter(() => new { ren, stimpy } ))
       // writes: { Start: CallingType = Characters | CallingMethod = Dinner | Subject = { ren = { FirstName = Ren, LastName = Hoek, Species = chihuahua, Weight = 1 } | stimpy = { FirstName = Stimpy, LastName = [null], Species = cat, Weight = 5 } } | TimeStamp = 2014-12-18 16:11:00Z }
{
     ren.Starve();
     stimpy.Feed();  

} // writes:
  // { Stop: CallingType = Characters | CallingMethod = Dinner | ElapsedMilliseconds = 1402 | Subject = { ren = { FirstName = Ren, LastName = Hoek, Species = chihuahua, Weight = .5 } | stimpy = { FirstName = Stimpy, LastName = [null], Species = cat, Weight = 9 } } | TimeStamp = 2014-12-18 16:11:02Z }
```

Confirm checkpoints along the way:

```csharp
using (var activity = Log.Enter(() => new { ren, stimpy } ))
{
    stimpy.Feed();  
    activity.Confirm(() => "Stimpy fed");

    ren.Starve();
    activity.Confirm(() => "Ren starved");

    throw new Exception("I want real food!"); 
    
    activity.Confirm(() => "Done!");
} // writes:
  // { Stop: CallingType = Characters | CallingMethod = Dinner | ElapsedMilliseconds = 1402 | Subject = { ren = { FirstName = Ren, LastName = Hoek, Species = chihuahua, Weight = .5 } | stimpy = { FirstName = Stimpy, LastName = [null], Species = cat, Weight = 9 } } | TimeStamp = 2014-12-18 16:11:02Z | Params = { { Confirmed = "Stimpy fed", "Ren starved"} } }
```

Format anything with the `ToLogString` extension method:

```csharp
Console.WriteLine(episodes.ToLogString());
// if episodes is null, it writes: [null]
// otherwise it writes using ToString: { SampleApp.Episode,  SampleApp.Episode,  SampleApp.Episode, (...12 more) }
```

Tell it to be more informative:

```csharp
Formatter<Episode>.RegisterForAllProperties();
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

...or buffer and aggregate them...

```csharp
var countOf404s = logEvents
                    .Where(e => e.SubjectIs<HttpException>())
                    .Where(e => e.GetSubject<HttpException>().GetHttpCode() == 404)
                    .Buffer(TimeSpan.FromSeconds(1))
                    .Select(es => es.Count());
```

### Objects, not strings

Its.Log works best when you pass objects rather than strings. 

This works:

```csharp
  Log.Write(() => "An error occurred: " + anException.ToString());
```

But this is better:

```csharp
  Log.Write(() => anException);
```

Knowing the type lets you decide at a policy level how to route them, format them, or react to them. It also helps keep your log output more consistent and your logging code more terse.

### Log levels - Info, Error, Warning, Verbose

While log levels can be specified, Its.Log also sets them by convention based on the type of the object you pass. For example, if the subject of a log entry is an exception:

```csharp
  Log.Write(() => anException);
```

then the output log level will default to `Error`:

```
  { Error: CallingType = Demo | CallingMethod = Formatting3_Exception_formatting_is_very_descriptive_by_default | ExceptionId = 75743ddd-824f-4c1b-954c-09b249d00e90 | Subject = { ReflectionTypeLoadException: Types = { Demo } | LoaderExceptions = { { DataException: Message = oops! | Data = { [why?, because] } | InnerException = [null] | TargetSite = [null] | StackTrace = [null] | HelpLink = [null] | Source = [null] | HResult = -2146232032 } } | Message = Exception of type 'System.Reflection.ReflectionTypeLoadException' was thrown. | Data = {  } | InnerException = [null] | TargetSite = Void Formatting3_Exception_formatting_is_very_descriptive_by_default() | StackTrace =    at Its.Log.Instrumentation.UnitTests.Demo.Formatting3_Exception_formatting_is_very_descriptive_by_default() in c:\dev\github\Its.Log\Its.Log.UnitTests\Demo.cs:line 93 | HelpLink = [null] | Source = Its.Log.UnitTests | HResult = -2146232830 } | TimeStamp = 2014-12-18 16:23:28Z | Params = {  } }
```

### JSON

Outputting logs as JSON rather than Its.Log's default format is simple. Here's an example using NewtonSoft.Json:

```csharp
  var serializer = new JsonSerializer
                        {
                           TypeNameHandling = TypeNameHandling.All,
                           ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                        };
                      
                        Formatter<LogEntry>.Register((entry, writer) =>
                        {
                          serializer.Serialize(writer, entry);
                          writer.WriteLine();
                        });
```
