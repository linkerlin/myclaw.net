using MyClaw.Core.Entities;

namespace MyClaw.Core.Tests.Entities;

public class EntityStoreTests : IDisposable
{
    private readonly string _testWorkspace;
    private readonly EntityStore _store;
    private DateTime _now = new(2026, 1, 10);

    public EntityStoreTests()
    {
        _testWorkspace = Path.Combine(Path.GetTempPath(), $"myclaw_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testWorkspace);
        _store = new EntityStore(_testWorkspace, () => _now);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testWorkspace))
        {
            Directory.Delete(_testWorkspace, true);
        }
    }

    [Fact]
    public async Task AddAsync_NewEntity_ShouldCreateWithDefaults()
    {
        var entity = new Entity
        {
            Name = "TestProject",
            Type = EntityType.Project,
            Attributes = new Dictionary<string, string> { { "language", "C#" } },
            Relations = new List<string> { "uses .NET" }
        };

        var result = await _store.AddAsync(entity);

        Assert.Equal("TestProject", result.Name);
        Assert.Equal(EntityType.Project, result.Type);
        Assert.Equal(1, result.MentionCount);
        Assert.Equal(100, result.Vitality);
        Assert.NotEmpty(result.FirstMentioned);
        Assert.NotEmpty(result.LastMentioned);
        Assert.Equal("2026-01-10", result.VitalityUpdatedAt);
        Assert.Single(result.Attributes);
        Assert.Single(result.Relations);
    }

    [Fact]
    public async Task AddAsync_ExistingEntity_ShouldUpdateMentionCount()
    {
        var entity = new Entity
        {
            Name = "TestPerson",
            Type = EntityType.Person,
            Attributes = new Dictionary<string, string>()
        };

        await _store.AddAsync(entity);
        var result = await _store.AddAsync(entity);

        Assert.Equal(2, result.MentionCount);
        Assert.Equal(100, result.Vitality);
    }

    [Fact]
    public async Task AddAsync_ExistingEntity_ShouldMergeAttributes()
    {
        var entity1 = new Entity
        {
            Name = "TestTool",
            Type = EntityType.Tool,
            Attributes = new Dictionary<string, string> { { "version", "1.0" } }
        };

        var entity2 = new Entity
        {
            Name = "TestTool",
            Type = EntityType.Tool,
            Attributes = new Dictionary<string, string> { { "license", "MIT" } }
        };

        await _store.AddAsync(entity1);
        var result = await _store.AddAsync(entity2);

        Assert.Equal(2, result.Attributes.Count);
        Assert.Equal("1.0", result.Attributes["version"]);
        Assert.Equal("MIT", result.Attributes["license"]);
    }

    [Fact]
    public async Task AddAsync_ExistingEntity_ShouldMergeRelations()
    {
        var entity1 = new Entity
        {
            Name = "TestConcept",
            Type = EntityType.Concept,
            Relations = new List<string> { "related to AI" }
        };

        var entity2 = new Entity
        {
            Name = "TestConcept",
            Type = EntityType.Concept,
            Relations = new List<string> { "used in ML" }
        };

        await _store.AddAsync(entity1);
        var result = await _store.AddAsync(entity2);

        Assert.Equal(2, result.Relations.Count);
    }

    [Fact]
    public async Task RemoveAsync_ExistingEntity_ShouldReturnTrue()
    {
        var entity = new Entity
        {
            Name = "ToRemove",
            Type = EntityType.Other
        };

        await _store.AddAsync(entity);
        var result = await _store.RemoveAsync("ToRemove");

        Assert.True(result);
        var count = await _store.GetCountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task RemoveAsync_NonExistingEntity_ShouldReturnFalse()
    {
        var result = await _store.RemoveAsync("NonExisting");
        Assert.False(result);
    }

    [Fact]
    public async Task LinkAsync_ExistingEntity_ShouldAddRelation()
    {
        var entity = new Entity
        {
            Name = "TestProject",
            Type = EntityType.Project
        };

        await _store.AddAsync(entity);
        var result = await _store.LinkAsync("TestProject", "depends on EntityFramework");

        Assert.True(result);
        var query = await _store.QueryAsync("TestProject");
        Assert.Contains("depends on EntityFramework", query!.Relations);
    }

    [Fact]
    public async Task QueryAsync_ExistingEntity_ShouldReturnEntity()
    {
        var entity = new Entity
        {
            Name = "QueryTest",
            Type = EntityType.Place,
            Attributes = new Dictionary<string, string> { { "location", "Beijing" } }
        };

        await _store.AddAsync(entity);
        var result = await _store.QueryAsync("QueryTest");

        Assert.NotNull(result);
        Assert.Equal("QueryTest", result.Name);
        Assert.Equal(EntityType.Place, result.Type);
    }

    [Fact]
    public async Task QueryAsync_NonExistingEntity_ShouldReturnNull()
    {
        var result = await _store.QueryAsync("NonExisting");
        Assert.Null(result);
    }

    [Fact]
    public async Task ListAsync_WithFilter_ShouldReturnFilteredEntities()
    {
        await _store.AddAsync(new Entity { Name = "Person1", Type = EntityType.Person });
        await _store.AddAsync(new Entity { Name = "Person2", Type = EntityType.Person });
        await _store.AddAsync(new Entity { Name = "Project1", Type = EntityType.Project });

        var persons = await _store.ListAsync(EntityType.Person);
        var projects = await _store.ListAsync(EntityType.Project);

        Assert.Equal(2, persons.Count);
        Assert.Single(projects);
    }

    [Fact]
    public async Task ListAsync_WithoutFilter_ShouldReturnAllEntities()
    {
        await _store.AddAsync(new Entity { Name = "Entity1", Type = EntityType.Tool });
        await _store.AddAsync(new Entity { Name = "Entity2", Type = EntityType.Concept });

        var all = await _store.ListAsync();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task QueryAsync_ShouldDecayVitalityByElapsedDays()
    {
        await _store.AddAsync(new Entity { Name = "DecayMe", Type = EntityType.Other });
        _now = _now.AddDays(3);

        var result = await _store.QueryAsync("DecayMe");

        Assert.NotNull(result);
        Assert.Equal(70, result!.Vitality);
        Assert.Equal("2026-01-13", result.VitalityUpdatedAt);
    }

    [Fact]
    public async Task AddAsync_ExistingEntity_ShouldRestoreVitalityWhenMentionedAgain()
    {
        await _store.AddAsync(new Entity { Name = "Recurring", Type = EntityType.Other });
        _now = _now.AddDays(4);

        var result = await _store.AddAsync(new Entity { Name = "Recurring", Type = EntityType.Other });

        Assert.Equal(2, result.MentionCount);
        Assert.Equal(85, result.Vitality);
        Assert.Equal("2026-01-14", result.LastMentioned);
        Assert.Equal("2026-01-14", result.VitalityUpdatedAt);
    }

    [Fact]
    public async Task ListAsync_ShouldRemoveEntitiesWhenVitalityDropsToZero()
    {
        await _store.AddAsync(new Entity { Name = "Ephemeral", Type = EntityType.Other });
        _now = _now.AddDays(10);

        var entities = await _store.ListAsync();

        Assert.Empty(entities);
        Assert.Equal(0, await _store.GetCountAsync());
    }

    [Fact]
    public async Task SurfaceRelevantAsync_WithMatchingText_ShouldReturnRelevantEntities()
    {
        await _store.AddAsync(new Entity { Name = "Docker", Type = EntityType.Tool });
        await _store.AddAsync(new Entity { Name = "Kubernetes", Type = EntityType.Tool });
        await _store.AddAsync(new Entity { Name = "MyProject", Type = EntityType.Project });

        var text = "I use Docker and Kubernetes for deployment";
        var relevant = await _store.SurfaceRelevantAsync(text);

        Assert.Equal(2, relevant.Count);
        Assert.Contains(relevant, e => e.Name == "Docker");
        Assert.Contains(relevant, e => e.Name == "Kubernetes");
    }

    [Fact]
    public async Task SurfaceRelevantAsync_ShouldSortByMentionCount()
    {
        var docker = new Entity { Name = "Docker", Type = EntityType.Tool };
        var kubernetes = new Entity { Name = "Kubernetes", Type = EntityType.Tool };

        await _store.AddAsync(docker);
        await _store.AddAsync(kubernetes);
        await _store.AddAsync(docker); // Docker mentioned twice
        await _store.AddAsync(docker); // Docker mentioned three times

        var text = "Docker Kubernetes";
        var relevant = await _store.SurfaceRelevantAsync(text);

        Assert.Equal("Docker", relevant[0].Name); // Higher mention count first
        Assert.Equal(3, relevant[0].MentionCount);
    }

    [Fact]
    public async Task SurfaceRelevantAsync_ShouldLimitTo5Entities()
    {
        for (int i = 0; i < 10; i++)
        {
            await _store.AddAsync(new Entity { Name = $"Tool{i}", Type = EntityType.Tool });
        }

        var text = string.Join(" ", Enumerable.Range(0, 10).Select(i => $"Tool{i}"));
        var relevant = await _store.SurfaceRelevantAsync(text);

        Assert.Equal(5, relevant.Count); // Max 5 entities
    }

    [Fact]
    public async Task AllTypes_ShouldBeSupported()
    {
        await _store.AddAsync(new Entity { Name = "Person", Type = EntityType.Person });
        await _store.AddAsync(new Entity { Name = "Project", Type = EntityType.Project });
        await _store.AddAsync(new Entity { Name = "Tool", Type = EntityType.Tool });
        await _store.AddAsync(new Entity { Name = "Concept", Type = EntityType.Concept });
        await _store.AddAsync(new Entity { Name = "Place", Type = EntityType.Place });
        await _store.AddAsync(new Entity { Name = "Other", Type = EntityType.Other });

        var count = await _store.GetCountAsync();
        Assert.Equal(6, count);
    }

    [Fact]
    public async Task CaseInsensitive_NameMatching()
    {
        await _store.AddAsync(new Entity { Name = "TestEntity", Type = EntityType.Tool });

        var queryLower = await _store.QueryAsync("testentity");
        var queryUpper = await _store.QueryAsync("TESTENTITY");
        var queryMixed = await _store.QueryAsync("TestEntity");

        Assert.NotNull(queryLower);
        Assert.NotNull(queryUpper);
        Assert.NotNull(queryMixed);
    }
}
