namespace GameStore.DAL.Entities
{
    public class PasswordResetToken
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(1);

        public bool IsUsed { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;
    }
}
