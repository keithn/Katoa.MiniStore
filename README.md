# Katoa.MiniStore

This is an embarassingly Simple .NET Key Value store using SQLite. Even though it's quite simple, it leverages the power of the SQLite engine and provies a really quick way to store data.  With some crafty key naming you can handle some semi complex data relationships.


## Nuget

You can find it https://www.nuget.org/packages/Katoa.MiniStore/

## How to use it

### Create / Open a store

```C#
var store = new MiniStore("test.db")
```

This will create ```test.db``` if it does not exist, otherwise it will open the existing store.

### Storing and Retrieving Data

```C#
store.Put("key", "some text")
var text = store.Get("key");
```

Both the key and value are strings, so it's completely flexible what you want to use for either

### Storing and Retrieving .NET objects

```C#
public record Foo(decimal Number, DateTime Time);

store.Put("foo", new Foo(1.234m, DateTime.UtcNow))
var foo = store.Get<Foo>("foo");
```
Quite often you want to store objects, so there are versions of Put and Get that will serialize and deserialize objects into JSON

### Checking if Data exists

```C#
if(store.Exists("key)) { }
```
Simple way to check if a key exists

### Storing Lots of Data at Once 

```C#
_store.BatchPut(new[] {
            ("foo-1", "first"),
            ("foo-2", "second")
        });
```
Putting a lot of data in SQLite can be quite slow if done individually, but putting a lot of data in a batch is VERY quick comparitively.  Go ahead, try it with thousands at once.  

### Deleting Data

```C#
store.Delete("key");
```
Deletes the key and value

### Getting all the Keys

```C#
store.Keys();
```

Gives you all the keys!

### Searching for Keys


```C#
store.Put("foo-1", "first");
store.Put("foo-2", "second");
store.Put("bar-1", "not having a");
store.KeysLike("foo%");
```

To get a bit more cunning with how use the store, you can use a key naming scheme + search to find subsets of data.  The pattern matching uses SQLites LIKE syntax 


