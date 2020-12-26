using Microsoft.AspNetCore.Server.HttpSys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace task_5
{
    public class TicTacToe
    {
        public Guid GameId { get; set; }

        public bool IsTaken { get; set; }

        public bool IsGameOver { get; set; }

        public bool IsDraw { get; set; }

        public Player Player1 { get; set; }

        public Player Player2 { get; set; }

        private readonly int[] field = new int[9];
        private int movesLeft = 9;

        public TicTacToe()
        {
            //Reset game
            for(int i = 0; i < field.Length; i++)
            {
                field[i] = -1;
            }
        }

        /// <summary>
        /// Insert a marker at a given position for a given player.
        /// </summary>
        /// <param name="player">The player number shoud be 0 or 1</param>
        /// <param name="position">The position where to place a marker, should be between 0 and 9</param>
        /// <returns>True if a winner was found(NOT A DRAW!)</returns>
        public bool Play(int player, int position)
        {
            if (IsGameOver)
            {
                return false;
            }

            PlaceMarker(player, position);

            return CheckWinner();
        }

        /// <summary>
        /// Insert a marker at a given position for a given player while game is not over
        /// </summary>
        /// <param name="player">The player number shoud be 0 or 1</param>
        /// <param name="position">The position where to place a marker, should be between 0 and 9</param>
        /// <returns>True if the marker position was not already taken</returns>
        private bool PlaceMarker(int player, int position)
        {
            movesLeft -= 1;  

            if(movesLeft <= 0)
            {
                IsGameOver = true;
                IsDraw = true;
                return false;
            }

            if(position > field.Length)
            {
                return false; //TODO mb exception here
            }
            //position is already taken
            if(field[position] != -1)
            {
                return false;
            }

            field[position] = player;

            return true;
        }

        /// <summary>
        /// Checks each different combination of marker placements and looks for a winner
        /// Each position is marked with an initial -1 which means no marker has yet been placed
        /// </summary>
        /// <returns></returns>
        private bool CheckWinner()
        {
            for(int i = 0; i < 3; ++i)
            {
                if ((field[i * 3] != -1 && field[(i * 3)] == field[(i * 3) + 1] && field[(i * 3)] == field[(i * 3) + 2]) ||
                     (field[i] != -1 && field[i] == field[i + 3] && field[i] == field[i + 6]))
                {
                    IsGameOver = true;
                    return true;
                }

            }

            if ((field[0] != -1 && field[0] == field[4] && field[0] == field[8]) || (field[2] != -1 && field[2] == field[4] && field[2] == field[6]))
            {
                IsGameOver = true;
                return true;
            }

            return false;
        }
    }
}
