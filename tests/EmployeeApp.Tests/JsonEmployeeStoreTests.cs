using System.Text.Json;
using EmployeeApp.Core.Models;
using EmployeeApp.Core.Persistence;

namespace EmployeeApp.Tests;

/// <summary>
/// 4.4 Async tests: the test method returns <c>async Task</c> (never async void) so xUnit awaits it.
/// IDisposable: xUnit calls Dispose after each test, which deletes that test's temp file.
/// </summary>
public sealed class JsonEmployeeStoreTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"employees-test-{Guid.NewGuid():N}.json");
    private readonly JsonEmployeeStore _store;

    public JsonEmployeeStoreTests() => _store = new JsonEmployeeStore(_path);

    public void Dispose()
    {
        if (File.Exists(_path))
            File.Delete(_path);
    }

    [Fact]
    public async Task SaveThenLoad_RoundTripsTypesAndAllFields()
    {
        // Arrange
        var original = TestData.CreateService().GetEmployees();

        // Act
        await _store.SaveAsync(original);
        var loaded = await _store.LoadAsync();

        // Assert: same people, same concrete types, same data
        Assert.Equal(original.Count, loaded.Count);
        var priya = Assert.IsType<FullTimeEmployee>(loaded.Single(e => e.Id == 2));
        Assert.Equal(90000m, priya.MonthlySalary);
        Assert.Equal(1, priya.ManagerId);
        Assert.IsType<Remote>(priya.Location);
        Assert.Equal(new Address("Chennai", "600001"), priya.Address);   // record: compared by value
        Assert.IsType<Manager>(loaded.Single(e => e.Id == 1));
        Assert.IsType<Intern>(loaded.Single(e => e.Id == 3));
    }

    [Fact]
    public async Task SaveAsync_AlreadyCancelledToken_ThrowsAndWritesNothing()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // ThrowsAnyAsync: also accepts TaskCanceledException, which derives from OperationCanceledException
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _store.SaveAsync(TestData.CreateService().GetEmployees(), cts.Token));

        Assert.False(File.Exists(_path));
    }

    [Fact]
    public async Task LoadAsync_AlreadyCancelledToken_Throws()
    {
        await _store.SaveAsync(TestData.CreateService().GetEmployees());
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _store.LoadAsync(cts.Token));
    }

    [Fact]
    public async Task LoadAsync_NoFileYet_ThrowsFileNotFound()
    {
        await Assert.ThrowsAsync<FileNotFoundException>(() => _store.LoadAsync());
    }

    [Fact]
    public async Task LoadAsync_UnknownEmployeeType_ThrowsJsonException()
    {
        await File.WriteAllTextAsync(_path,
            """[{ "Id": 1, "Type": "Robot", "Name": "X", "DepartmentCode": "ENG", "MonthlySalary": 1, "Location": "Office", "JoinedOn": "2026-01-01T00:00:00" }]""");

        await Assert.ThrowsAsync<JsonException>(() => _store.LoadAsync());
    }

    [Fact]
    public async Task LoadedEmployees_CanReplaceServiceData()
    {
        // Arrange: save the seed data, then change the live service
        var service = TestData.CreateService();
        await _store.SaveAsync(service.GetEmployees());
        service.GiveRaise(2, 10);

        // Act: load the saved snapshot back in
        service.ReplaceAllEmployees(await _store.LoadAsync());

        // Assert: the raise made after saving is gone, and the list is still in Id order
        Assert.Equal(90000m, service.GetEmployee(2).MonthlySalary);
        Assert.Equal([1, 2, 3, 4, 5], service.GetEmployees().Select(e => e.Id));
    }
}
