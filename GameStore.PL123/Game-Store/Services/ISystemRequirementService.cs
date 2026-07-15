namespace GameStore.PL.Services;

public class RequirementModel
{
    public string? OS { get; set; }
    public string? Processor { get; set; }
    public string? Memory { get; set; }
    public string? Graphics { get; set; }
    public string? DirectX { get; set; }
    public string? Storage { get; set; }
    public string? Network { get; set; }
    public string? Notes { get; set; }
}

public class SystemRequirementsModel
{
    public RequirementModel Minimum { get; set; } = new();
    public RequirementModel Recommended { get; set; } = new();
}

public interface ISystemRequirementService
{
    Task<SystemRequirementsModel?> GetAsync(string gameId);
    Task SaveAsync(string gameId, SystemRequirementsModel model);
}
