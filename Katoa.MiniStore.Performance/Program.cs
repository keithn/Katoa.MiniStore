using System.Diagnostics;
using Katoa.MiniStore;


var path = "performance.db";
MiniStore.DeleteStore(path);
var store = new MiniStore(path + "");

const int batchInserts = 10000;
const int puts = 2000;
const int gets = batchInserts+puts;

var sw = new Stopwatch();


// Batch Insers
sw.Start();

var list = new List<(string, string)>();
for(int i=0; i<batchInserts;i++)
{
    list.Add(($"Key{i}", $"Value{i}"));
}
store.BatchPut(list);
sw.Stop();
Console.WriteLine($"Batch Inserts - Took {sw.ElapsedMilliseconds}ms to insert {batchInserts} items");

// Puts
sw.Reset();
sw.Start();

for(int i=batchInserts; i<puts+batchInserts;i++)
{
    store.Put($"Key{i}", $"Value{i}");
}
sw.Stop();
Console.WriteLine($"Puts - Took {sw.ElapsedMilliseconds}ms to insert {puts} items");

// Gets
sw.Reset();
sw.Start();
List<string> values = new List<string>();
for(int i=0; i<gets;i++)
{
    values.Add(store.Get($"Key{i}"));
}

sw.Stop();

Console.WriteLine($"Gets - Took {sw.ElapsedMilliseconds}ms to get {gets} items");