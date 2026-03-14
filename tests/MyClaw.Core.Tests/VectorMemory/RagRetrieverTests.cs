using MyClaw.Core.VectorMemory;

namespace MyClaw.Core.Tests.VectorMemory;

public class RagRetrieverTests
{
    private static (RagRetriever retriever, InMemoryVectorStore store) CreateRetriever(int dimension = 128)
    {
        var store = new InMemoryVectorStore(dimension);
        var embeddingService = new SimpleEmbeddingService(dimension);
        var retriever = new RagRetriever(store, embeddingService);
        return (retriever, store);
    }

    [Fact]
    public async Task IndexAsync_ShouldAddEntryToStore()
    {
        var (retriever, store) = CreateRetriever();

        await retriever.IndexAsync("Test content", "test", importance: 0.5);

        Assert.Equal(1, store.Count);
    }

    [Fact]
    public async Task IndexAsync_ShouldChunkLongContent()
    {
        var (retriever, store) = CreateRetriever();
        var longContent = new string('a', 1000); // 超过默认分块大小

        await retriever.IndexAsync(longContent, "test");

        // 长文本应该被分成多个块
        Assert.True(store.Count >= 1);
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnRelevantResults()
    {
        var (retriever, store) = CreateRetriever();

        await retriever.IndexAsync("The quick brown fox jumps over the lazy dog", "test");
        await retriever.IndexAsync("Database systems store and retrieve data efficiently", "test");

        var result = await retriever.SearchAsync("fox jumps", topK: 2);

        Assert.NotEmpty(result.Sources);
        Assert.Contains("fox", result.Sources[0].Entry.Content);
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnContext()
    {
        var (retriever, _) = CreateRetriever();

        await retriever.IndexAsync("Important fact about cats", "test");

        var result = await retriever.SearchAsync("cats", topK: 1);

        Assert.NotEmpty(result.Context);
        Assert.Contains("cats", result.Context);
    }

    [Fact]
    public async Task SearchAsync_ShouldTrackTiming()
    {
        var (retriever, _) = CreateRetriever();

        await retriever.IndexAsync("Test content", "test");

        var result = await retriever.SearchAsync("test");

        Assert.True(result.ElapsedMs >= 0);
        Assert.True(result.EmbeddingMs >= 0);
        Assert.True(result.SearchMs >= 0);
    }

    [Fact]
    public async Task IndexBatchAsync_ShouldIndexMultipleDocuments()
    {
        var (retriever, store) = CreateRetriever();
        var documents = new[]
        {
            new DocumentToIndex { Content = "Document 1", SourceType = "test" },
            new DocumentToIndex { Content = "Document 2", SourceType = "test" },
            new DocumentToIndex { Content = "Document 3", SourceType = "test" }
        };

        var count = await retriever.IndexBatchAsync(documents);

        Assert.Equal(3, count);
        Assert.Equal(3, store.Count);
    }

    [Fact]
    public async Task HybridSearchAsync_ShouldCombineSemanticAndKeyword()
    {
        var (retriever, _) = CreateRetriever();

        await retriever.IndexAsync("Python is a programming language", "test");
        await retriever.IndexAsync("JavaScript is also a programming language", "test");

        var result = await retriever.HybridSearchAsync("Python programming", topK: 2);

        Assert.NotEmpty(result.Sources);
        // Python 相关文档应该排在前面
        Assert.Contains("Python", result.Sources[0].Entry.Content);
    }

    [Fact]
    public async Task GetRelevantContextAsync_ShouldReturnFormattedContext()
    {
        var (retriever, _) = CreateRetriever();

        await retriever.IndexAsync("Important information about testing", "test");

        var context = await retriever.GetRelevantContextAsync("testing");

        Assert.Contains("# 相关记忆", context);
        Assert.Contains("testing", context);
    }

    [Fact]
    public async Task GetRelevantContextAsync_ShouldRespectMaxTokens()
    {
        var (retriever, _) = CreateRetriever();

        // 添加多个长文档
        for (int i = 0; i < 10; i++)
        {
            await retriever.IndexAsync(new string('a', 500), "test");
        }

        var context = await retriever.GetRelevantContextAsync("a", maxTokens: 100);

        // 应该被截断
        Assert.True(context.Length < 2000);
    }

    [Fact]
    public async Task SearchAsync_ShouldFilterByMinScore()
    {
        var (retriever, _) = CreateRetriever();

        await retriever.IndexAsync("Completely unrelated content about xyz", "test");

        var result = await retriever.SearchAsync("python programming", topK: 5, minScore: 0.99);

        // 高阈值应该返回很少或没有结果
        Assert.True(result.Sources.Count <= 1);
    }

    [Fact]
    public async Task IndexAsync_ShouldStoreMetadata()
    {
        var (retriever, store) = CreateRetriever();
        var metadata = new Dictionary<string, string>
        {
            ["category"] = "important",
            ["author"] = "test"
        };

        await retriever.IndexAsync("Test content", "test", metadata: metadata);

        var entries = store.GetAllEntries().ToList();
        Assert.Single(entries);
        Assert.Equal("important", entries[0].Metadata["category"]);
        Assert.Equal("test", entries[0].Metadata["author"]);
    }
}

public class RagOptionsTests
{
    [Fact]
    public void DefaultValues_ShouldBeReasonable()
    {
        var options = new RagOptions();

        Assert.Equal(500, options.MaxChunkSize);
        Assert.Equal(50, options.ChunkOverlap);
        Assert.Equal(5, options.DefaultTopK);
        Assert.Equal(0.3, options.DefaultMinScore);
    }
}

public class DocumentToIndexTests
{
    [Fact]
    public void DefaultValues_ShouldBeSet()
    {
        var doc = new DocumentToIndex();

        Assert.Equal(string.Empty, doc.Content);
        Assert.Equal("unknown", doc.SourceType);
        Assert.Equal(0.5, doc.Importance);
    }
}

public class VectorSearchTypesTests
{
    [Fact]
    public void VectorSearchRequest_DefaultValues_ShouldBeSet()
    {
        var request = new VectorSearchRequest();

        Assert.Equal(string.Empty, request.Query);
        Assert.Equal(5, request.TopK);
        Assert.Equal(0.0, request.MinScore);
    }

    [Fact]
    public void VectorSearchResult_DefaultValues_ShouldBeSet()
    {
        var result = new VectorSearchResult();

        Assert.NotNull(result.Entry);
        Assert.Equal(0, result.Score);
        Assert.Equal(0, result.ElapsedMs);
    }
}
