namespace GameStore.PL.Pages
{
    public class ClaudeModel : PageModel
    {
        private readonly ClaudeService _claudeService;

        [BindProperty]
        public string Prompt { get; set; } = string.Empty;

        public string ClaudeResponse { get; set; } = string.Empty;

        public ClaudeModel(ClaudeService claudeService)
        {
            _claudeService = claudeService;
        }

        public async Task OnPostAsync()
        {
            if (!string.IsNullOrWhiteSpace(Prompt))
            {
                ClaudeResponse = await _claudeService.AskAsync(Prompt);
            }
        }
    }
}
