using Jellyking.Core.Models;

namespace Jellyking.Core.Services;

public sealed class JsonUserStore : JsonStoreBase<User>, IUserStore
{
    private readonly string _filePath;

    public JsonUserStore(string dataDirectory)
    {
        _filePath = Path.Combine(dataDirectory, "users.json");
    }

    protected override string FilePath => _filePath;

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default)
    {
        return await LoadAsync(ct);
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var all = await LoadAsync(ct);
        return all.FirstOrDefault(u => u.Id == id);
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        var all = await LoadAsync(ct);
        return all.FirstOrDefault(u =>
            string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<bool> AnyAsync(CancellationToken ct = default)
    {
        var all = await LoadAsync(ct);
        return all.Count > 0;
    }

    public async Task<User> AddAsync(User user, CancellationToken ct = default)
    {
        var all = (await LoadAsync(ct)).ToList();
        user.Id = Guid.NewGuid();
        user.CreatedAt = DateTimeOffset.UtcNow;
        user.UpdatedAt = user.CreatedAt;
        all.Add(user);
        await SaveAsync(all, ct);
        return user;
    }

    public async Task<User> UpdateAsync(User user, CancellationToken ct = default)
    {
        var all = (await LoadAsync(ct)).ToList();
        var idx = all.FindIndex(u => u.Id == user.Id);
        if (idx < 0)
            throw new InvalidOperationException($"User {user.Id} not found.");

        user.UpdatedAt = DateTimeOffset.UtcNow;
        all[idx] = user;
        await SaveAsync(all, ct);
        return user;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var all = (await LoadAsync(ct)).ToList();
        var removed = all.RemoveAll(u => u.Id == id);
        if (removed > 0)
            await SaveAsync(all, ct);
    }
}
