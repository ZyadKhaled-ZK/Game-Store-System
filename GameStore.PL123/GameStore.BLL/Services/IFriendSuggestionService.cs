namespace GameStore.BLL.Services;

public interface IFriendSuggestionService
{
    Task<List<(User User, int MutualGamesCount)>> GetSuggestionsAsync(string userId, int count = 6);
}