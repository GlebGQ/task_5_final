using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace task_5.Services
{

    public class GameArgs : EventArgs
    {
        public Guid GameId { get; set; }
    }
    public class GameService
    {
        private ConcurrentDictionary<Player, object> players = new ConcurrentDictionary<Player, object>();
        private ConcurrentDictionary<TicTacToe, object> games = new ConcurrentDictionary<TicTacToe, object>();
        private Random random = new Random();
        private object trash = new object();

        public delegate void NotifyClients(GameArgs args);
        public event NotifyClients OnGameDelete;

        public List<TicTacToe> GetAllAvailableGames()
        {
            return games.Where(g => !g.Key.IsTaken).Select(g => g.Key).ToList();
        }
        public List<TicTacToe> GetGamesByTags(List<string> tags)
        {
            return games.Where(g =>
            {
                bool isContainsAllTags = tags.All(t => g.Key.Tags.Contains(t));
                bool isNotTaken = !g.Key.IsTaken;
                return isContainsAllTags && isNotTaken;
            }).Select(g => g.Key).ToList();
        }
        public void DeleteUser(string userName, string connectionId)
        {
            var player = players.FirstOrDefault(p => p.Key.Name == userName).Key;
            if (IsPlayerInGame(userName))
            {
                player.ConnectionIds.RemoveWhere(c => c.Equals(connectionId));
            }
            else
            {
                players.TryRemove(player,out trash);
            }
        }
        public TicTacToe TryCreateGame(string userName, List<string> tags, string gameName)
        {
            var player = players.FirstOrDefault(p => p.Key.Name == userName).Key;
            if (IsPlayerInGame(userName)) return null;
            if (player != null)
            {
                TicTacToe game = new TicTacToe
                {
                    GameId = Guid.NewGuid(),
                    GameName = gameName,
                    Tags = tags,
                    Player1 = player,
                    IsTaken = false
                };
                player.LookingForOpponent = true;
                games.TryAdd(game, null);
                return game;
            }
            return null;
        }
        public void JoinGame(string gameId, string userName)
        {
            var game = games.FirstOrDefault(g => g.Key.GameId == Guid.Parse(gameId) && g.Key.IsTaken == false).Key;
            if (game != null)
            {
                game.IsTaken = true;
                Player creator = game.Player1;
                Player opponent = players.FirstOrDefault(p => p.Key.Name == userName).Key;

                game.Player2 = opponent;

                creator.IsPlaying = true;
                opponent.IsPlaying = true;

                creator.Opponent = opponent;
                opponent.Opponent = creator;
            }
        }
        public TicTacToe GetGame(string gameId)
        {
            var game = games.FirstOrDefault(g => g.Key.GameId == Guid.Parse(gameId)).Key;
            return game;
        }
        public bool RegisterPlayer(string userName, string connectionId)
        {
            var player = players.FirstOrDefault(p => p.Key.Name == userName).Key;
            if (player == null)
            {
                player = new Player
                {
                    Name = userName
                };
                player.ConnectionIds.Add(connectionId);
                player.IsPlaying = false;
                players.TryAdd(player, null);
                return true;
            }
            else
            {
                player.ConnectionIds.Add(connectionId);
            }

            return false;
        }
        public bool IsPlayerInGame(string userName)
        {
            var game = games.FirstOrDefault(g => g.Key.Player1?.Name == userName || g.Key.Player2?.Name == userName).Key;
            if (game == null)
            {
                return false;
            }

            return true;
        }
        public Player GetPlayer(string userName)
        {
            return players.FirstOrDefault(p => p.Key.Name == userName).Key;
        }
        public void ClearConnectionIds(string userName)
        {
            var player = players.FirstOrDefault(x => x.Key.Name == userName).Key;
            player.ConnectionIds.Clear();
        }
        public void DeletePlayerWithoutGame(string userName)
        {
            var clientWithoutGame = players.FirstOrDefault(x => x.Key.Name == userName).Key;
            if (clientWithoutGame != null)
            {
                players.TryRemove(clientWithoutGame, out trash);

            }
        }
        public void RemoveGame(string userName) {
            var game = games.FirstOrDefault(g => g.Key.Player1.Name == userName || g.Key.Player2.Name == userName).Key;
            if (game != null)
            {
                OnGameDelete?.Invoke(new GameArgs { GameId = game.GameId});
                games.TryRemove(game, out trash);
            }
        }
        public void UpdateOpponentInfoWhenDisconnecting(string userName)
        {
            var player = players.FirstOrDefault(p => p.Key.Name == userName).Key;
            if (player.Opponent != null)
            {
                player.Opponent.IsPlaying = false;
                player.Opponent.WaitingForMove = false;
                player.Opponent.LookingForOpponent = false;
            }
        }
        public Player TryFindOpponent(string userName)
        {
            var player = players.FirstOrDefault(p => p.Key.Name == userName).Key;
            //if (player == null) return;

            player.LookingForOpponent = true;

            var opponent = players.Where(p => p.Key.Name != userName && p.Key.LookingForOpponent && !p.Key.IsPlaying).OrderBy(p => Guid.NewGuid()).FirstOrDefault().Key;

            return opponent;
        }
        public void UpdateRivalsInfo(string playerName, string opponentName)
        {
            var player = players.FirstOrDefault(p => p.Key.Name == playerName).Key;
            var opponent = players.FirstOrDefault(p => p.Key.Name == opponentName).Key;

            player.IsPlaying = true;
            player.LookingForOpponent = false;
            opponent.IsPlaying = true;
            opponent.LookingForOpponent = false;

            player.Opponent = opponent;
            opponent.Opponent = player;
        }
        public bool IsPlayerMakesMoveFirst(string playerName, string opponentName)
        {
            var player = players.FirstOrDefault(p => p.Key.Name == playerName).Key;
            var opponent = players.FirstOrDefault(p => p.Key.Name == opponentName).Key;

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
        public bool IsGameExists(string playerName)
        {
            var game = games.FirstOrDefault(g => g.Key.Player1.Name == playerName || g.Key.Player2.Name == playerName).Key;

            if (game == null || game.IsGameOver) return false;

            return true;
        }
        public int DetectConnectedPlayerInGame(string playerName)
        {
            var game = games.FirstOrDefault(g => g.Key.Player1.Name == playerName || g.Key.Player2.Name == playerName).Key;

            if (game.Player2.Name == playerName)
            {
                return 1;
            }

            return 0;
        }
        public TicTacToe FindGame(string userName)
        {
            return games.FirstOrDefault(g => g.Key.Player1?.Name == userName || g.Key.Player2?.Name == userName).Key;

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
