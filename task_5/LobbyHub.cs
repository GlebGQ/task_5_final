using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using task_5.Services;

namespace task_5
{
    public class LobbyHub : Hub
    {
        private GameService gameService;

        public LobbyHub(GameService gameService)
        {
            this.gameService = gameService;
        }

        public async Task GetAllAvailableGames()
        {
            List<TicTacToe> games = gameService.GetAllAvailableGames();
            await Clients.Caller.SendAsync("gamesFound", games.Count);
        }

        public async Task CreateGame()
        {
            bool isCreated = gameService.TryCreateGame(Context.ConnectionId);
            if (isCreated)
            {
                await Clients.All.SendAsync("newGameCreated");
            }
        }
    }
}
