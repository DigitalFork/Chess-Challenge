using ChessChallenge.API;
using static ChessChallenge.API.BitboardHelper;
using System;
using static System.Math;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

namespace ChessChallenge.Example {
    public class MyBot : IChessBot {

        readonly int[] PieceValues = { 0, 100, 300, 300, 500, 900, 100000 };

        Timer timer;
        Move RootBestMove;
        const int MaxIterations = 20;
        const int MaxTime = 1000;

        (ulong Key, int SearchDepth, int Eval, Move Move, bool FullSearch)[] transposition = new (ulong, int, int, Move, bool)[1048576];

        public Move Think(Board board, Timer turnTimer) {
            timer = turnTimer;
            bool isWhite = board.IsWhiteToMove;
            RootBestMove = Move.NullMove;
            int eval = -1073741823;
            int alpha = eval;
            int beta = -eval;
            bool fullSearch = false;

            int currentDepth = 0;
            while (currentDepth < 7 && timer.MillisecondsElapsedThisTurn < MaxTime) {
                (Move newBestMove, int newEval) = GetBestMove(board, currentDepth, 0, alpha, beta, fullSearch);
                if (newEval <= alpha || newEval >= beta || newBestMove == Move.NullMove) {
                    alpha = -1073741823;
                    beta = 1073741823;
                    fullSearch = true;
                    continue;
                }

                alpha = newEval - 100;
                beta = newEval + 100;
                fullSearch = false;
                currentDepth++;
                RootBestMove = newBestMove;
                eval = newEval;
            }

            return RootBestMove;
        }
        (Move, int) GetBestMove(Board board, int depth, int iterations, int alpha, int beta, bool fullSearch) {

            ulong index = board.ZobristKey % 1048576;

            if (board.IsInCheckmate()) {
                return (Move.NullMove, -536870911);
            }
            if (board.IsDraw()) {
                return (Move.NullMove, 0);
            }


            if (transposition[index].Key == board.ZobristKey && transposition[index].SearchDepth >= depth && (!fullSearch || transposition[index].FullSearch)) {
                return (transposition[index].Move, transposition[index].Eval);
            }
            
            if (depth < 0 || iterations >= MaxIterations) {
                return (Move.NullMove, Evaluate(board));
            }

            Move bestMove = Move.NullMove;

            Move[] moves = OrderMoves(board, board.GetLegalMoves(), iterations == 0 ? RootBestMove : transposition[index].Move);



            foreach (Move move in moves) {
                int newEval = -1073741823;
                board.MakeMove(move);
                (Move newMove, newEval) = GetBestMove(board, depth - 1, iterations + 1, -beta, -alpha, fullSearch);
                newEval = -newEval;

                board.UndoMove(move);

                if (timer.MillisecondsElapsedThisTurn >= MaxTime) {
                    if (iterations == 0) { break; } else { return (Move.NullMove, 0); };
                }


                if (newEval >= beta) {
                    return (move, beta);
                }
                if (newEval > alpha) {
                    alpha = newEval;
                    bestMove = move;
                }

            }
            if (timer.MillisecondsElapsedThisTurn < MaxTime) { transposition[index] = (board.ZobristKey, depth, alpha, bestMove, fullSearch); }
            return (bestMove, alpha);
        }
        Move[] OrderMoves(Board board, Move[] moves, Move firstMove) {

            int[] scoreGuess = new int[moves.Length];
            bool inCheck = board.IsInCheck();

            for (int i = 0; i < moves.Length; i++) {
                Move move = moves[i];

                board.MakeMove(move);
                scoreGuess[i] -= (board.IsInCheck() ? 200 : 0)
                                + (move.IsPromotion ? PieceValues[(int)move.PromotionPieceType] : 0)
                                + (move.IsCapture ? scoreGuess[i] -= PieceValues[(int)move.CapturePieceType] - PieceValues[(int)move.MovePieceType] : 0)
                                + (move == firstMove ? 10000 : 0)
                                - ((GetPawnAttacks(move.StartSquare, !board.IsWhiteToMove) & board.GetPieceBitboard(PieceType.Pawn, board.IsWhiteToMove)) > 0 ? 500 : 0);
                board.UndoMove(move);
            }
            Array.Sort(scoreGuess, moves);

            return moves.Where(move => move != Move.NullMove).ToArray();
        }
        int Evaluate(Board board) {
            int eval = 0;

            double endgameWeight = EndGameWeight(board);

            foreach (PieceList pieceList in board.GetAllPieceLists()) {
                // Subtract when indexing piecelists because PieceValues includes "PieceType.None" and GetAllPieceLists() doesn't
                eval += pieceList.Count * PieceValues[(int)pieceList.TypeOfPieceInList];
            }
            return board.IsWhiteToMove ? eval : -eval;
        }

        double EndGameWeight(Board board) {
            return Min(8.0 / board.GetAllPieceLists().Sum(x => x.Count), 1);
        }
        int ChebyshevDist(Square square) {
            return Max(CenterDist(square.Rank), CenterDist(square.File));
        }

        int CenterDist(int row) {
            return Max(3 - row, row - 4);
        }
        int Lerp(int a, int b, double t) {
            return (int)(a + (b - a) * t);
        }

    }
}