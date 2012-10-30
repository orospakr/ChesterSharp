# ChesterSharp

Copyright (C) 2012 Andrew Clunis <andrew@orospakr.ca>

Licensed under Apache v2 (see COPYING.txt for details)

Asynchronous CouchDB access library in C#, for .net 4.5 and using the
new HttpClient library.

Has no external dependencies except for James Newton-King's json.net
library.

I've written it on Mono 2.11.4 on Linux (and have not yet tested it on
Microsoft), although I don't see much likelihood of trouble.

# Usage

Please note that all of the API is async, in that it returns Tasks
that can be either `await`ed (C#'s continuations) or passed a lambda
with `ContinueWith` to invoke when ready.

Open a database:

```csharp
Couch couchdb = new ChesterSharp.Couch("localhost", 5984);
CouchDatabase myDatabase = await couchdb.OpenDatabase("mydatabase");
```

Fetch a document from CouchDB, as a string:

```csharp
String myDocument = await myDatabase.GetRawDocument("documentid");
```

Fetch a document from CouchDB, with a POCO type:

```csharp
public class Person : ChesterSharp.Documents.CouchDocument {
    public String Name { get; set; }
}

Person person = await myDatabase.GetDocument<Person>("personid");
```

Create a document in CouchDB, with a POCO type (reusing type above):

```csharp
var person = new Person { Name = "Ludwig von Mises", Id = "vonmises" };

// It will update the POCO object with the newly acquired CouchDB
// generated rev (and ID if none was specified), and return it

await myDatabase.CreateDocument<Person>(person);
```

Update an existing document:

```csharp
var person = await myDatabase.GetDocument<Person>("vonmises");
person.Name = "Ludvig von Mises, economist";

await myDatabase.UpdateDocument<Person>(person);
```

Create (or replace) a design document:

```csharp
// in addition to the POCO above, make a seprate
// design document class:

public class PersonDesign : ChesterSharp.Documents.DesignDocument {
    // Views on the design document are defined by special nested
    // classes.
    public class All : View {
        public override string Map { get {
                return @"function(doc) {
                    if(doc[""type""] === ""person"") {
                        emit(doc[""_id""], doc.title);
                    }
                }";
            }
        }
    }
}

await myDatabase.UpdateDesignDocument<PersonDesign>();
```

Fetch a view:

```csharp
// note that, for now, the system always uses include_docs, and
// deserializes the result into the provided CouchDocument type (the
// third type argument).  Anything directly emit()ted is currently
// ignored.

IEnumerable<Person> persons = await myDatabase.GetDocsFromView<PersonDesign, PersonDesign.Living, Person>();
```

And more.  See the tests in `ChesterSharp/CouchTest.cs` for
further details.

# Building

1. Initialize and fetch the dependency git submodules:

    git submodule init
    git submodule update

2. Open the included MonoDevelop/MSBuild solution file.  If that gives
   trouble, try making a new solution including the
   `ChesterSharp/ChesterSharp.csproj` project and the JSON.net
   project.
   
3. You may have to adjust the target .net framework on the
   NewtonSoft.Json project to be ".NET 4.0", if you're using
   MonoDevelop.
   
3. Run the included tests (requires an NUnit test harness).  Note that
   these are currently integration tests, and require an running
   CouchDB in order to work.

# Using it in your own projects

In lieu of better arrangements for linking (or submission to NuGet,
which is really only useful for Windows users at any rate), your best
option for linking against ChesterSharp is to either:

## Add it as a Git submodule to your project

This is the method I use.

1. Create the submodule:

    git submodule init
    git submodule add git://github.com/orospakr/chestersharp

2. Add the `ChesterSharp/ChesterSharp.csproj` project to your solution
   (either in MD or in Visual Studio);
   
3. Furnish the dependency on JSON.net yourself, to taste (as an
   additional submodule, assembly reference, or so on).

## Reference a built assembly manually

Build it as per the Building instructions above, and reference the
resulting assembly in your application.

# API Documentation

I haven't yet generated any, sorry.

# Plans/Ideas:

* changes feed (whee HTTP streaming)
* changes feed with specified view
* list revisions, conflict resolution?
* attachments
* compare current design documents in system, update if necessary
* declarative data validation on CouchDocuments, since couchdb apps
  should be responsible themselves for incoming data sanity, if
  possible, so might as well make it convenient.  DataContract stuff
  possible?
* sequence numbering? seems like a local rememberance -- at least
  across the lifetime of a given consumer of events that wants
  updates.  have it instantiate an object that remembers seq, and does
  both fetching of content and holding changes listeners? should be
  pretty much the only instance of concurrent access to data in the
  entire library.  wait. seq is across the entire database.  that
  means have it on a single database connection, regardless of view
* streaming results: instead of buffering up results into a List<> before
  returning, offer IEnumerable<>s that populate. Is this a good idea?
* GetView should have a seprate version that can be fed a 
  derivative POCO type for ViewResultRowValue type that gets the
  fields emitted from emit() in the couchdb map function.  
* some sort of simple ORM/Model: view-level mapping to DTOs (built in
  find/findAll() routines, etc.)

