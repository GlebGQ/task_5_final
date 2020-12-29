using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using task_5.Models;
using task_5.Services;

namespace task_5
{

    public class LobbyHub : Hub
    {
        private GameService gameService;
        private IHubContext<LobbyHub> hubContext;

        public LobbyHub(GameService gameService, IHubContext<LobbyHub> hubContext)
        {
            this.hubContext = hubContext;
            this.gameService = gameService;
            gameService.OnGameDelete += (gameArgs) => {
                this.hubContext.Clients.All.SendAsync("gameisClosed", gameArgs.GameId);
            };

        }
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            gameService.DeleteUser(Context.User.Identity.Name, Context.ConnectionId);

            await base.OnDisconnectedAsync(exception);
        }
        public async Task RegistrateUser(string userName)
        {
            gameService.RegisterPlayer(userName, Context.ConnectionId);
            await Clients.Caller.SendAsync("registrationComplete");
        }
        public async Task GetGamesByTags(List<string> tags)
        {
            List<TicTacToe> filteredGames = new List<TicTacToe>();
            if (tags.Count == 0)
            {
                filteredGames = gameService.GetAllAvailableGames();
            }
            else {
                filteredGames = gameService.GetGamesByTags(tags);
            } 
            List<GamePreview> gamePreviews = filteredGames.Select(g => new GamePreview
            {
                GameId = g.GameId.ToString(),
                Tags = g.Tags,
                GameName = g.GameName,
                CreatorName = (g.Player1.Name == null) ? g.Player2.Name : g.Player1.Name

            }).ToList();
            await Clients.Caller.SendAsync("updateGameList", gamePreviews);
        }
        public async Task GetAllAvailableGames()
        {
            List<TicTacToe> games = gameService.GetAllAvailableGames();
            List<GamePreview> gamePreviews = games.Select(g => new GamePreview
            {
                GameName = g.GameName,
                Tags = g.Tags,
                GameId = g.GameId.ToString(),
                CreatorName = (g.Player1.Name == null) ? g.Player2.Name : g.Player1.Name
            }).ToList();
            await Clients.Caller.SendAsync("gamesFound", gamePreviews);
        }
        public async Task CreateGame(List<string> tags, string gameName)
        {
            TicTacToe createdGame = gameService.TryCreateGame(Context.User.Identity.Name, tags, gameName);
            if (createdGame != null)
            {
                await Clients.Caller.SendAsync("redirectToWaitingScreen", createdGame.GameId.ToString());
                await Clients.Others.SendAsync("newGameCreated", new GamePreview { 
                    GameId = createdGame.ToString(),
                    CreatorName = Context.User.Identity.Name
                });;
            }
        }
        public async Task JoinGame(string gameId)
        {
            var player = gameService.GetPlayer(Context.User.Identity.Name);
            gameService.JoinGame(gameId, Context.User.Identity.Name);
            await Clients.AllExcept(player.ConnectionIds.ToList())
                .SendAsync("gameIsClosed", gameId);
            await Clients.Caller.SendAsync("connectedToTheGame", gameId);
        }

    }
}
