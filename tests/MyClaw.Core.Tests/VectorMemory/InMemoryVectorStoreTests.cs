using MyClaw.Core.VectorMemory;

namespace MyClaw.Core.Tests.VectorMemory;

public class InMemoryVectorStoreTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithCorrectDimension()
    {
        var store = new InMemoryVectorStore(384);

        Assert.Equal(384, store.Dimension);
        Assert.Equal(0, store.Count);
    }

    [Fact]
    public async Task UpsertAsync_ShouldAddEntry()
    {
        var store = new InMemoryVectorStore(128);
        var entry = new VectorMemoryEntry
        {
            Content = "Test content",
            Embedding = new float[128],
            SourceType = "test"
        };

        var id = await store.UpsertAsync(entry);

        Assert.NotNull(id);
        Assert.NotEmpty(id);
        Assert.Equal(1, store.Count);
    }

    [Fact]
    public async Task UpsertAsync_ShouldGenerateIdIfNotProvided()
    {
        var store = new InMemoryVectorStore(128);
        var entry = new VectorMemoryEntry
        {
            Content = "Test",
            Embedding = new float[128]
        };

        var id = await store.UpsertAsync(entry);

        Assert.NotNull(id);
        Assert.Equal(id, entry.Id);
    }

    [Fact]
    public async Task UpsertAsync_ShouldUpdateExistingEntry()
    {
        var store = new InMemoryVectorStore(128);
        var entry = new VectorMemoryEntry
        {
            Id = "test-id",
            Content = "Original",
            Embedding = new float[128]
        };

        await store.UpsertAsync(entry);

        entry.Content = "Updated";
        await store.UpsertAsync(entry);

        var retrieved = await store.GetAsync("test-id");
        Assert.Equal("Updated", retrieved?.Content);
        Assert.Equal(1, store.Count);
    }

    [Fact]
    public async Task UpsertBatchAsync_ShouldAddMultipleEntries()
    {
        var store = new InMemoryVectorStore(128);
        var entries = new[]
        {
            new VectorMemoryEntry { Content = "Entry 1", Embedding = new float[128] },
            new VectorMemoryEntry { Content = "Entry 2", Embedding = new float[128] },
            new VectorMemoryEntry { Content = "Entry 3", Embedding = new float[128] }
        };

        var count = await store.UpsertBatchAsync(entries);

        Assert.Equal(3, count);
        Assert.Equal(3, store.Count);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnEntry()
    {
        var store = new InMemoryVectorStore(128);
        var entry = new VectorMemoryEntry
        {
            Id = "test-id",
            Content = "Test content",
            Embedding = new float[128]
        };

        await store.UpsertAsync(entry);
        var retrieved = await store.GetAsync("test-id");

        Assert.NotNull(retrieved);
        Assert.Equal("test-id", retrieved.Id);
        Assert.Equal("Test content", retrieved.Content);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnNullForNonExistent()
    {
        var store = new InMemoryVectorStore(128);

        var result = await store.GetAsync("non-existent");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_ShouldUpdateAccessStats()
    {
        var store = new InMemoryVectorStore(128);
        var entry = new VectorMemoryEntry
        {
            Id = "test-id",
            Content = "Test",
            Embedding = new float[128]
        };

        await store.UpsertAsync(entry);
        await store.GetAsync("test-id");
        await store.GetAsync("test-id");

        var retrieved = await store.GetAsync("test-id");
        Assert.Equal(3, retrieved?.AccessCount);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveEntry()
    {
        var store = new InMemoryVectorStore(128);
        var entry = new VectorMemoryEntry
        {
            Id = "test-id",
            Content = "Test",
            Embedding = new float[128]
        };

        await store.UpsertAsync(entry);
        var deleted = await store.DeleteAsync("test-id");

        Assert.True(deleted);
        Assert.Equal(0, store.Count);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalseForNonExistent()
    {
        var store = new InMemoryVectorStore(128);

        var result = await store.DeleteAsync("non-existent");

        Assert.False(result);
    }

    [Fact]
    public async Task ClearAsync_ShouldRemoveAllEntries()
    {
        var store = new InMemoryVectorStore(128);
        await store.UpsertBatchAsync(new[]
        {
            new VectorMemoryEntry { Embedding = new float[128] },
            new VectorMemoryEntry { Embedding = new float[128] },
            new VectorMemoryEntry { Embedding = new float[128] }
        });

        await store.ClearAsync();

        Assert.Equal(0, store.Count);
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnSimilarEntries()
    {
        var store = new InMemoryVectorStore(128);

        // 创建两个相似的向量
        var vector1 = new float[128];
        var vector2 = new float[128];
        var queryVector = new float[128];

        // 设置一些非零值使向量相似
        for (int i = 0; i < 10; i++)
        {
            vector1[i] = 0.5f;
            vector2[i] = 0.5f;
            queryVector[i] = 0.5f;
        }

        await store.UpsertAsync(new VectorMemoryEntry
        {
            Id = "entry1",
            Content = "Entry 1",
            Embedding = vector1,
            SourceType = "test"
        });

        await store.UpsertAsync(new VectorMemoryEntry
        {
            Id = "entry2",
            Content = "Entry 2",
            Embedding = vector2,
            SourceType = "test"
        });

        var request = new VectorSearchRequest
        {
            QueryVector = queryVector,
            TopK = 5,
            MinScore = 0.5
        };

        var results = await store.SearchAsync(request);

        Assert.Equal(2, results.Count);
        Assert.True(results[0].Score > 0.9);
    }

    [Fact]
    public async Task SearchAsync_ShouldFilterBySourceType()
    {
        var store = new InMemoryVectorStore(128);
        var vector = new float[128];
        vector[0] = 1.0f;

        await store.UpsertAsync(new VectorMemoryEntry
        {
            Id = "entry1",
            Content = "Entry 1",
            Embedding = vector,
            SourceType = "type_a"
        });

        await store.UpsertAsync(new VectorMemoryEntry
        {
            Id = "entry2",
            Content = "Entry 2",
            Embedding = vector,
            SourceType = "type_b"
        });

        var request = new VectorSearchRequest
        {
            QueryVector = vector,
            TopK = 5,
            SourceTypes = new List<string> { "type_a" }
        };

        var results = await store.SearchAsync(request);

        Assert.Single(results);
        Assert.Equal("type_a", results[0].Entry.SourceType);
    }

    [Fact]
    public async Task SearchAsync_ShouldFilterByMetadata()
    {
        var store = new InMemoryVectorStore(128);
        var vector = new float[128];
        vector[0] = 1.0f;

        await store.UpsertAsync(new VectorMemoryEntry
        {
            Id = "entry1",
            Content = "Entry 1",
            Embedding = vector,
            Metadata = new Dictionary<string, string> { ["category"] = "important" }
        });

        await store.UpsertAsync(new VectorMemoryEntry
        {
            Id = "entry2",
            Content = "Entry 2",
            Embedding = vector,
            Metadata = new Dictionary<string, string> { ["category"] = "normal" }
        });

        var request = new VectorSearchRequest
        {
            QueryVector = vector,
            TopK = 5,
            MetadataFilter = new Dictionary<string, string> { ["category"] = "important" }
        };

        var results = await store.SearchAsync(request);

        Assert.Single(results);
        Assert.Equal("important", results[0].Entry.Metadata["category"]);
    }

    [Fact]
    public async Task SearchAsync_ShouldRespectMinScore()
    {
        var store = new InMemoryVectorStore(128);

        // 创建两个不同的向量
        var vector1 = new float[128];
        var vector2 = new float[128];
        vector1[0] = 1.0f;
        vector2[127] = 1.0f;

        await store.UpsertAsync(new VectorMemoryEntry
        {
            Content = "Entry",
            Embedding = vector1
        });

        var request = new VectorSearchRequest
        {
            QueryVector = vector2,
            TopK = 5,
            MinScore = 0.99
        };

        var results = await store.SearchAsync(request);

        Assert.Empty(results);
    }

    [Fact]
    public async Task SaveAndLoadAsync_ShouldPersistData()
    {
        var store = new InMemoryVectorStore(128);
        var tempPath = Path.GetTempFileName();

        try
        {
            await store.UpsertAsync(new VectorMemoryEntry
            {
                Id = "test-id",
                Content = "Test content",
                Embedding = new float[128],
                SourceType = "test"
            });

            await store.SaveAsync(tempPath);

            var newStore = new InMemoryVectorStore(128);
            await newStore.LoadAsync(tempPath);

            Assert.Equal(1, newStore.Count);
            var entry = await newStore.GetAsync("test-id");
            Assert.Equal("Test content", entry?.Content);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    [Fact]
    public void SearchWithVector_ShouldReturnOrderedResults()
    {
        var store = new InMemoryVectorStore(128);

        // 创建相似度不同的向量
        var queryVector = new float[128];
        var highSimilarity = new float[128];
        var lowSimilarity = new float[128];

        for (int i = 0; i < 10; i++)
        {
            queryVector[i] = 1.0f;
            highSimilarity[i] = 1.0f;
            lowSimilarity[i] = 0.1f;
        }

        store.UpsertAsync(new VectorMemoryEntry
        {
            Id = "high",
            Content = "High similarity",
            Embedding = highSimilarity
        }).Wait();

        store.UpsertAsync(new VectorMemoryEntry
        {
            Id = "low",
            Content = "Low similarity",
            Embedding = lowSimilarity
        }).Wait();

        var results = store.SearchWithVector(queryVector, topK: 5);

        Assert.Equal(2, results.Count);
        Assert.Equal("high", results[0].Entry.Id);
        Assert.True(results[0].Score > results[1].Score);
    }
}
