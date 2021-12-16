using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Katoa.MiniStore.Tests;

public class MiniStoreTests : IDisposable
{
    private readonly MiniStore _store;

    public MiniStoreTests()
    {
        const string testDb = "test.db";
        MiniStore.DeleteStore(testDb);
        _store = new MiniStore(testDb);
    }

    [Fact]
    public void PutGet()
    {
        _store.Put("Test", "Value");
        _store.Get("Test").Should().Be("Value");
    }

    record TestRecord(int Id, decimal Amount);
    
    [Fact]
    public void PutGetTyped()
    {
        _store.Put("x",new TestRecord(1, 2.5m));
        _store.Get<TestRecord>("x").Should().Be(new TestRecord(1, 2.5m));
    }

    [Fact]
    public void PutUpdateGet()
    {
        _store.Put("Test", "Value");
        _store.Put("Test", "Value2");
        _store.Get("Test").Should().Be("Value2");
    }

    [Fact]
    public void GetNonExisting()
    {
        _store.Get("DoesntExist").Should().BeEmpty();
    }

    [Fact]
    public void ExistsOrNot()
    {
        _store.Put("Test", "Value");
        _store.Exists("DoesntExist").Should().BeFalse();
        _store.Exists("Test").Should().BeTrue();
    }

    [Fact]
    public void Delete()
    {
        _store.Put("Test", "Value");
        _store.Exists("Test").Should().BeTrue();
        _store.Delete("Test");
        _store.Exists("Test").Should().BeFalse();
    }

    [Fact]
    public void Keys()
    {
        _store.Put("Test", "Value");
        _store.Put("Case", "Another");
        _store.Keys().Should()
            .HaveCount(2)
            .And.Contain(new[] { "Test", "Case" });
    }
    [Fact]
    public void KeysLike()
    {
        _store.Put("Test", "Value");
        _store.Put("Test2", "Value2");
        _store.Put("Case", "Another");
        _store.KeysLike("Test%").Should()
            .HaveCount(2)
            .And.Contain(new[] { "Test", "Test2" })
            .And.NotContain("Case");
        
    }

    [Fact]
    public void BatchPut()
    {
        _store.BatchPut(new[] {
            ("Test", "Value"),
            ("Case", "Another")
        });
        _store.Get("Test").Should().Be("Value");
        _store.Get("Case").Should().Be("Another");
    } 
    
    public void Dispose()
    {
        // Used to release the connection pools hold on the database file so that the next test can recreate the file.
        SqliteConnection.ClearAllPools();
    }
}