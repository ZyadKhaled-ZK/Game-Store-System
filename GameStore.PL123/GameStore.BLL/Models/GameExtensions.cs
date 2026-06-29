using GameStore.DAL.Entities;

namespace GameStore.BLL.Models
{
    public static class GameExtensions
    {
        public static decimal GetEffectivePrice(this Game game, IEnumerable<Sale> activeSales)
        {
            var sale = activeSales?.FirstOrDefault(s => s.GameId == game.Id);
            return sale != null ? sale.NewPrice : game.Price;
        }
    }
}
