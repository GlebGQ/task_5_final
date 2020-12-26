using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace task_5.Services
{
    public class GameService
    {
        private ConcurrentDictionary<Player, object> players = new ConcurrentDictionary<Player, object>();
        private ConcurrentDictionary<TicTacToe, object> games = new ConcurrentDictionary<TicTacToe, object>();
        private Random random = new Random();
        private object trash = new object();


        public List<TicTacToe> GetAllAvailableGames()
        {
            return games.Where(g => !g.Key.IsTaken).Select(g => g.Key).ToList();
        }


        public bool TryCreateGame(string connectionId)
        {
            var player = players.FirstOrDefault(p => p.Key.ConnectionId == connectionId).Key;
            if (IsPlayerInGame(connectionId)) return false;
            if(player != null)
            {
                TicTacToe game = new TicTacToe
                {
                    Player1 = player,
                    IsTaken = false
                };
            }
            return false;
        }

        public bool RegisterPlayer(string userName, string connectionId)
        {
            var player = players.FirstOrDefault(p => p.Key.ConnectionId == connectionId).Key;
            if (player == null)
            {
                player = new Player
                {
                    Name = userName,
                    ConnectionId = connectionId
                };

                player.IsPlaying = false;
                players.TryAdd(player, null);
                return true;
            }

            return false;
        }

        public bool IsPlayerInGame(string connectionId)
        {
            var game = games.FirstOrDefault(g => g.Key.Player1.ConnectionId == connectionId || g.Key.Player2.ConnectionId == connectionId).Key;
            if (game == null)
            {
                return false;
            }

            return true;
        }

        public Player GetPlayer (string connectionId)
        {
            return players.FirstOrDefault(p => p.Key.ConnectionId == connectionId).Key;
        }

        public void DeletePlayerWithoutGame(string connectionId)
        {
            var clientWithoutGame = players.FirstOrDefault(x => x.Key.ConnectionId == connectionId).Key;
            if (clientWithoutGame != null)
            {
                players.TryRemove(clientWithoutGame, out trash);

            }
        }

        public void RemoveGame(string connectionId) {
            var game = games.FirstOrDefault(g => g.Key.Player1.ConnectionId == connectionId || g.Key.Player2.ConnectionId == connectionId).Key;
            if (game != null)
            {
                games.TryRemove(game, out trash);
            }
        }

        public void UpdateOpponentInfo(string connectionId)
        {
            var player = players.FirstOrDefault(p => p.Key.ConnectionId == connectionId).Key;
            if (player.Opponent != null)
            {
                player.Opponent.IsPlaying = false;
                player.Opponent.WaitingForMove = false;
                player.Opponent.LookingForOpponent = false;
            }
        }

        public Player TryFindOpponent(string connectionId)
        {
            var player = players.FirstOrDefault(p => p.Key.ConnectionId == connectionId).Key;
            //if (player == null) return;

            player.LookingForOpponent = true;

            var opponent = players.Where(p => p.Key.ConnectionId != connectionId && p.Key.LookingForOpponent && !p.Key.IsPlaying).OrderBy(p => Guid.NewGuid()).FirstOrDefault().Key;

            return opponent;
        }

        public void UpdateRivalsInfo(string playerConnectionId, string opponentConnectionId)
        {
            var player = players.FirstOrDefault(p => p.Key.ConnectionId == playerConnectionId).Key;
            var opponent = players.FirstOrDefault(p => p.Key.ConnectionId == opponentConnectionId).Key;

            player.IsPlaying = true;
            player.LookingForOpponent = false;
            opponent.IsPlaying = true;
            opponent.LookingForOpponent = false;

            player.Opponent = opponent;
            opponent.Opponent = player;
        }

        public bool IsPlayerMakesMoveFirst(string playerConnectionId, string opponentConnectionId)
        {
            var player = players.FirstOrDefault(p => p.Key.ConnectionId == playerConnectionId).Key;
            var opponent = players.FirstOrDefault(p => p.Key.ConnectionId == opponentConnectionId).Key;

            if (random.Next(0, 5000) % 2 == 0)
            {
                player.WaitingForMove = false;
                opponent.WaitingForMove = true;

                return true;
            }
            else
            {
                player.WaitingForMove = true;
                opponent.WaitingForMove = false;

                return false;
            }
        }

        public void CreateGame(string playerConnectionId, string opponentConnectionId)
        {
            var player = players.FirstOrDefault(p => p.Key.ConnectionId == playerConnectionId).Key;
            var opponent = players.FirstOrDefault(p => p.Key.ConnectionId == opponentConnectionId).Key;

            games.TryAdd(new TicTacToe
            {
                Player1 = player,
                Player2 = opponent
            }, null);
        }

        public bool IsGameExists(string connectionId)
        {
            var game = games.FirstOrDefault(g => g.Key.Player1.ConnectionId == connectionId || g.Key.Player2.ConnectionId == connectionId).Key;

            if (game == null || game.IsGameOver) return false;

            return true;
        }

        public int DetectConnectedPlayerInGame(string connectionId)
        {
            var game = games.FirstOrDefault(g => g.Key.Player1.ConnectionId == connectionId || g.Key.Player2.ConnectionId == connectionId).Key;

            if (game.Player2.ConnectionId == connectionId)
            {
                return 1;
            }

            return 0;
        }

        public TicTacToe FindGame(string connectionId)
        {
            return games.FirstOrDefault(g => g.Key.Player1.ConnectionId == connectionId || g.Key.Player2.ConnectionId == connectionId).Key;

        }

        public bool WinnerIsFound(TicTacToe game, int marker, int position)
        {
            if (game.Play(marker, position))
            {
                games.TryRemove(game, out trash);

                game.Player1.IsPlaying = false;
                game.Player2.IsPlaying = false;

                game.Player1.LookingForOpponent = false;
                game.Player2.LookingForOpponent = false;

                game.Player1.WaitingForMove = false;
                game.Player2.WaitingForMove = false;

                return true;
            }

            return false;
        }

        public bool IsDrawAfterMove(TicTacToe game)
        {
            if (game.IsGameOver && game.IsDraw)
            {
                game.Player1.IsPlaying = false;
                game.Player2.IsPlaying = false;

                game.Player1.LookingForOpponent = false;
                game.Player2.LookingForOpponent = false;

                game.Player1.WaitingForMove = false;
                game.Player2.WaitingForMove = false;

                games.TryRemove(game, out trash);
                return true;
            }

            return false;
        }

        public bool GameIsNotOver(TicTacToe game, Player player)
        {
            if (!game.IsGameOver)
            {
                player.WaitingForMove = !player.WaitingForMove;
                player.Opponent.WaitingForMove = !player.Opponent.WaitingForMove;

                return true;
            }

            return false;
        }
    }
}
