namespace GameStore.PL.Services;

public class SystemRequirementService : ISystemRequirementService
{
    private readonly IJsonFileStore _store;

    public SystemRequirementService(IJsonFileStore store)
    {
        _store = store;
    }

    public async Task<SystemRequirementsModel?> GetAsync(string gameId)
    {
        return await _store.ReadAsync<SystemRequirementsModel>($"requirements/{gameId}.json");
    }

    public async Task SaveAsync(string gameId, SystemRequirementsModel model)
    {
        await _store.WriteAsync($"requirements/{gameId}.json", model);
    }
}
