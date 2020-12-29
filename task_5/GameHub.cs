using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Pipes;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
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

        public HashSet<string> ConnectionIds { get; set; } = new HashSet<string>();
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
            
            var player = gameService.GetPlayer(Context.User.Identity.Name);
            var game = gameService.FindGame(player.Name);

            if (player == null || game == null)
            {
                await Clients.Caller.SendAsync("RedirectToLobby");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, Context.User.Identity.Name);
            await Clients.Group(Context.User.Identity.Name).SendAsync("registerComplete");
        }

        /// <summary>
        /// When a client disconnects remove the game and announce a walk-over if there's a game in place then the client is removed from the clients and game list
        /// </summary>
        /// <returns>If the operation takes long, run it asynchronously and return the task in which it runs</returns>
        public override async Task OnDisconnectedAsync(Exception ex)
        {
            Player playerToBeDisconnected = gameService.GetPlayer(Context.User.Identity.Name);

            if (playerToBeDisconnected != null)
            {
                if (!gameService.IsPlayerInGame(Context.User.Identity.Name))
                {
                    await Clients.Caller.SendAsync("RedirectToLobby");
                    gameService.DeletePlayerWithoutGame(Context.User.Identity.Name);
                }
                else { 
                    //    await Clients.Group(playerToBeDisconnected.Name).SendAsync("redirectToLobby");
                    await Clients.Caller.SendAsync("RedirectToLobby");
                    if(playerToBeDisconnected.Opponent != null)
                    {
                        await Clients.Group(playerToBeDisconnected.Opponent?.Name).SendAsync("OpponentDisconnected", playerToBeDisconnected.Name);
                    }
                    gameService.RemoveGame(playerToBeDisconnected.Name);
                    gameService.UpdateOpponentInfoWhenDisconnecting(playerToBeDisconnected.Name);
                    gameService.DeletePlayerWithoutGame(playerToBeDisconnected.Name);
                }

                foreach (var connectionId in playerToBeDisconnected.ConnectionIds)
                {
                    await Groups.RemoveFromGroupAsync(connectionId, playerToBeDisconnected.Name);
                }
            }
            await base.OnDisconnectedAsync(ex);
        }


        public async Task ConnectOpponents()
        {

            var opponent = gameService.GetPlayer(Context.User.Identity.Name).Opponent;
            
            //Player is waiting for an opponent to join
            if(opponent == null)
            {
                return;
            }

            await Clients.Group(Context.User.Identity.Name).SendAsync("FoundOpponent", opponent.Name);
            await Clients.Group(opponent.Name).SendAsync("FoundOpponent", Context.User.Identity.Name);


            if (gameService.IsPlayerMakesMoveFirst(Context.User.Identity.Name, opponent.Name))
            {
                await Clients.Group(Context.User.Identity.Name).SendAsync("WaitingForMarkerPlacement", opponent.Name);
                await Clients.Group(opponent.Name).SendAsync("WaitingForOpponent", Context.User.Identity.Name); //opponent.Name in the ex
            }
            else
            {
                await Clients.Group(opponent.Name).SendAsync("WaitingForMarkerPlacement", Context.User.Identity.Name);
                await Clients.Group(Context.User.Identity.Name).SendAsync("WaitingForOpponent", opponent.Name); //opponent.Name in the ex
            }

        }

        public async Task Play(string strPosition)
        {
            int position = Int32.Parse(strPosition);
            if (!gameService.IsGameExists(Context.User.Identity.Name))
            {
                return;
            }

            int marker = gameService.DetectConnectedPlayerInGame(Context.User.Identity.Name);

            TicTacToe game = gameService.FindGame(Context.User.Identity.Name);

            var player = marker == 0 ? game.Player1 : game.Player2;

            //If the player is waiting for the opponent but still tried to make a move, just return
            if (player.WaitingForMove) return;


            //Notify both players that a marker has been placed
            GameInformation gameInformation = new GameInformation
            {
                OpponentName = player.Name,
                MarkerPosition = position
            };
            await Clients.Group(game.Player1.Name).SendAsync("AddMarkerPlacement", gameInformation);
            await Clients.Group(game.Player2.Name).SendAsync("AddMarkerPlacement", gameInformation);


            if (gameService.WinnerIsFound(game, marker, position))
            {
                await Clients.Group(game.Player1.Name).SendAsync("GameOver", player.Name);
                await Clients.Group(game.Player2.Name).SendAsync("GameOver", player.Name);
                return;
            }

            if (gameService.IsDrawAfterMove(game))
            {

                await Clients.Group(game.Player1.Name).SendAsync("Draw", "It's a draw!");
                await Clients.Group(game.Player2.Name).SendAsync("Draw", "It's a draw!");
                return;
            }


            if (gameService.GameIsNotOver(game, player))
            {
                await Clients.Group(player.Opponent.Name).SendAsync("WaitingForMarkerPlacement", player.Name);
            }
        }
    }
}