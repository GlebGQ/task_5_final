using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Pipes;
using System.Linq;
using System.Threading.Tasks;
using task_5.Services;

namespace task_5
{
    public class Player
    {
        public string Name { get; set; }
        public Player Opponent { get; set; }
        public bool IsPlaying { get; set; }
        public bool WaitingForMove { get; set; }
        public bool LookingForOpponent { get; set; }

        public string ConnectionId { get; set; }
    }

    [Authorize]
    public class GameHub : Hub
    { 
        private GameService gameService;

        public GameHub(GameService gameService)
        {
            this.gameService = gameService;
        }

        public async Task RegisterPlayer()
        {
            bool isRegistrated = gameService.RegisterPlayer(Context.User.Identity.Name, Context.ConnectionId);

            if (isRegistrated)
            {
                await Clients.Client(Context.ConnectionId).SendAsync("registerComplete", Context.User.Identity.Name);
            }
            else
            {
                //TODO user is already created
            }
        }

        //public async Task GetAllAvailableGames()
        //{
        //    var availableGames = games.Where(g => g.Key.IsTaken == false).Select(g => g.Key).ToList();
        //    await Clients.Caller.SendAsync("gamesFound", availableGames);
        //}

        /// <summary>
        /// When a client disconnects remove the game and announce a walk-over if there's a game in place then the client is removed from the clients and game list
        /// </summary>
        /// <returns>If the operation takes long, run it asynchronously and return the task in which it runs</returns>
        public override async Task OnDisconnectedAsync(Exception ex)
        {
            if(!gameService.IsPlayerInGame(Context.ConnectionId))
            {
                gameService.DeletePlayerWithoutGame(Context.ConnectionId);
            }
            else
            {
                Player playerToBeDisconnected =  gameService.GetPlayer(Context.ConnectionId);
                await Clients.Client(playerToBeDisconnected.Opponent.ConnectionId).SendAsync("OpponentDisconnected", Context.User.Identity.Name);
                gameService.RemoveGame(Context.ConnectionId);
                gameService.UpdateOpponentInfo(Context.ConnectionId);
                gameService.DeletePlayerWithoutGame(Context.ConnectionId);
            }
            await base.OnDisconnectedAsync(ex);
        }


        public async Task FindOpponent()
        {
            Player opponent = gameService.TryFindOpponent(Context.ConnectionId);
           
            if (opponent == null)
            {
                await Clients.Client(Context.ConnectionId).SendAsync("NoOpponents");
                return;
            }

            gameService.UpdateRivalsInfo(Context.ConnectionId, opponent.ConnectionId);

            await Clients.Client(Context.ConnectionId).SendAsync("FoundOpponent", opponent.Name);
            await Clients.Client(opponent.ConnectionId).SendAsync("FoundOpponent", Context.User.Identity.Name);


            if (gameService.IsPlayerMakesMoveFirst(Context.ConnectionId, opponent.ConnectionId))
            { 
                await Clients.Client(Context.ConnectionId).SendAsync("WaitingForMarkerPlacement", opponent.Name);
                await Clients.Client(opponent.ConnectionId).SendAsync("WaitingForOpponent", Context.User.Identity.Name); //opponent.Name in the ex
            }
            else
            {
                await Clients.Client(opponent.ConnectionId).SendAsync("WaitingForMarkerPlacement", Context.User.Identity.Name);
                await Clients.Client(Context.ConnectionId).SendAsync("WaitingForOpponent", opponent.Name); //opponent.Name in the ex
            }

            gameService.CreateGame(Context.ConnectionId, opponent.ConnectionId);

        }

        public async Task Play(string strPosition)
        {
            int position = Int32.Parse(strPosition);
            if (!gameService.IsGameExists(Context.ConnectionId))
            {
                return;
            }

            int marker = gameService.DetectConnectedPlayerInGame(Context.ConnectionId);

            TicTacToe game = gameService.FindGame(Context.ConnectionId);

            var player = marker == 0 ? game.Player1 : game.Player2;

            //If the player is waiting for the opponent but still tried to make a move, just return
            if (player.WaitingForMove) return;


            //Notify both players that a marker has been placed
            GameInformation gameInformation = new GameInformation
            {
                OpponentName = player.Name,
                MarkerPosition = position
            };
            await Clients.Client(game.Player1.ConnectionId).SendAsync("AddMarkerPlacement", gameInformation);
            await Clients.Client(game.Player2.ConnectionId).SendAsync("AddMarkerPlacement", gameInformation);


            if (gameService.WinnerIsFound(game, marker, position))
            {
                await Clients.Client(game.Player1.ConnectionId).SendAsync("GameOver", player.Name);
                await Clients.Client(game.Player2.ConnectionId).SendAsync("GameOver", player.Name);
                return;
            }

            if (gameService.IsDrawAfterMove(game))
            {

                await Clients.Client(game.Player1.ConnectionId).SendAsync("GameOver", "It's a draw!");
                await Clients.Client(game.Player2.ConnectionId).SendAsync("GameOver", "It's a draw!");
                return;
            }


            if (gameService.GameIsNotOver(game, player))
            {
                await Clients.Client(player.Opponent.ConnectionId).SendAsync("WaitingForMarkerPlacement", player.Name);
            }
        }
    }
}