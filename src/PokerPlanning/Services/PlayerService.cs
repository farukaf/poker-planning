using Blazored.LocalStorage;

namespace PokerPlanning.Services
{
    public class PlayerService : IPlayerService
    {
        public PlayerService(ILocalStorageService localStorage)
        {
            this._localStorage = localStorage;
        }

        const string USER_NAME_KEY = "USER_NAME_KEY";
        private readonly ILocalStorageService _localStorage;

        public async Task<string> GetUserName()
        {
            return await _localStorage.GetItemAsync<string>(USER_NAME_KEY) ?? string.Empty;
        }

        public async Task SetUserName(string userName)
        {
            await _localStorage.SetItemAsStringAsync(USER_NAME_KEY, userName);
            PlayerNameChanged?.Invoke(this, new());
        }

        public EventHandler? PlayerNameChanged { get; set; }
    }
}
