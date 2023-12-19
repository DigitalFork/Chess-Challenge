/*using ChessChallenge.API;
using static ChessChallenge.API.BitboardHelper;
using System;
using static System.Math;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

public class MyBot : IChessBot {

    readonly int[] PieceValues = { 0, 100, 300, 300, 500, 900, 100000 };
    readonly int[] PawnPushValues = { 5, 5, 5, 10, 10, -10, 5, 5 };
    readonly int[] RookFileValues = { 0, 0, -10, -20 };
    readonly int[] KingFileValues = { -50, -25, 0, -5 };


    int positions;
    int transpositions;
    int transpositionOverwrites;
    int betaCutoffs;

    Timer timer;
    Move RootBestMove;
    const int MaxIterations = 20;
    const int MaxTime = 1000;

    (ulong Key,  int SearchDepth, int Eval, Move Move, bool FullSearch)[] transposition = new (ulong, int, int, Move, bool)[1048576];

    public Move Think(Board board, Timer turnTimer) {
        timer = turnTimer;
        *//*positions = 0;
        transpositions = 0;
        transpositionOverwrites = 0;
        betaCutoffs = 0; *//*
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
        Console.WriteLine("Depth Searched: {0}, time taken: {1}", currentDepth, timer.MillisecondsElapsedThisTurn);

        Console.WriteLine("Eval from Search: {0}", eval);
        *//*Console.WriteLine("Eval function before move: {0}", Evaluate(board));
        board.MakeMove(RootBestMove);
        Console.WriteLine("Eval function after move: {0}", -Evaluate(board));
        Console.WriteLine();
        *//*Console.WriteLine("Positions Evaluated: {0}", positions);*//* 
        Console.WriteLine("Transpositions Found: {0}", transpositions);
        Console.WriteLine("Transpositions Overwrote: {0}", transpositionOverwrites);
        Console.WriteLine("Beta cutoffs reached: {0}", betaCutoffs);
        Console.WriteLine();*//*

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
            transpositions++;
            return (transposition[index].Move, transposition[index].Eval);
        }

        Move bestMove = Move.NullMove;

        Move[] moves = OrderMoves(board, board.GetLegalMoves(depth < 0), iterations == 0 ? RootBestMove : transposition[index].Move);

        if (moves.Length == 0 || depth <= -4 || iterations >= MaxIterations) {
            return (Move.NullMove, Evaluate(board));
        }


        foreach (Move move in moves) {
            positions++;
            bool iterate = true;
            int newEval = -1073741823;
            board.MakeMove(move);
            if (depth < 0 && EndGameWeight(board) < 0.875) {

                int evaluation = -Evaluate(board);

                if (evaluation < alpha - 200) {
                    newEval = alpha;
                    iterate = false;
                }
                if (evaluation >= beta) {
                    newEval = beta;
                    iterate = false;
                }

                alpha = Max(alpha, evaluation);

            }
            if (iterate) {
                (Move newMove, newEval) = GetBestMove(board, depth - (board.IsInCheck() || move.IsPromotion ? 0 : 1), iterations + 1, -beta, -alpha, fullSearch);
                newEval = -newEval;
            }
            
            board.UndoMove(move);

            if (timer.MillisecondsElapsedThisTurn >= MaxTime) {
                if (iterations == 0) { break; } 
                else { return (Move.NullMove, 0); };
            }


            if (newEval >= beta) {
                betaCutoffs++;
                return (move, beta);
            }
            if (newEval > alpha) {
                alpha = newEval;
                bestMove = move;
            }

        }

        if (transposition[index].Key != 0) { transpositionOverwrites++; }
        if (timer.MillisecondsElapsedThisTurn < MaxTime) { transposition[index] = (board.ZobristKey, depth, alpha, bestMove, fullSearch); }
        return (bestMove, alpha);
    }
    Move[] OrderMoves(Board board, Move[] moves, Move firstMove) {
        
        int[] scoreGuess = new int[moves.Length];

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

        return moves;
    }
    int Evaluate(Board board) {
        int eval = 0;

        double endgameWeight = EndGameWeight(board);

        foreach (PieceList pieceList in board.GetAllPieceLists()) {
            // Subtract when indexing piecelists because PieceValues includes "PieceType.None" and GetAllPieceLists() doesn't
            foreach (Piece piece in pieceList) {
                int intPieceType = (int)piece.PieceType;
                int pieceEval = PieceValues[intPieceType];
                Square square = piece.Square;

                // Increase pieceEval for each square it controls. Central squares are weighted higher in the early game.

                int rank = piece.IsWhite ? square.Rank : 7 - square.Rank;
                int file = square.File;

                ulong pieceAttacks = GetPieceAttacks(piece.PieceType, square, board, piece.IsWhite);
                int squareAttackValues = 0;
                for (int i = 0; i < GetNumberOfSetBits(pieceAttacks); i++) {
                    Square attackSquare = new Square(ClearAndGetIndexOfLSB(ref pieceAttacks));
                    squareAttackValues += Lerp((attackSquare.Rank < 4 == piece.IsWhite ? 3 : 4) - ChebyshevDist(attackSquare), 4, endgameWeight);
                    
                }
                switch (intPieceType) {
                    case 1:
                        // Interpolate from a center pawn push to an even pawn push as the game progresses
                        pieceEval += rank * Lerp(PawnPushValues[file], 20, endgameWeight) + 5 * squareAttackValues;
                        break;
                    case 2: case 3:
                        pieceEval += GetNumberOfSetBits(pieceAttacks) * 5 + squareAttackValues;
                        break;
                    case 4:
                        pieceEval += Lerp(RookFileValues[CenterDist(file)], 0, endgameWeight);
                        break;
                    case 6:
                        pieceEval += Lerp(KingFileValues[CenterDist(file)] - rank * 30, 0, endgameWeight);
                        break;

                }
                eval += piece.IsWhite ? pieceEval : -pieceEval;
            }
        }

        Square whiteKingSquare = board.GetKingSquare(true);

        Square blackKingSquare = board.GetKingSquare(false);

        int cornerKingEval = 7 - Max(Abs(whiteKingSquare.Rank - blackKingSquare.Rank), Abs(whiteKingSquare.File - blackKingSquare.File));

        eval += Lerp(0,
            20 * (eval > 200 ? ChebyshevDist(blackKingSquare) + cornerKingEval
            : (eval < -200) ? ChebyshevDist(whiteKingSquare) - cornerKingEval
            : 0), endgameWeight * endgameWeight);
        return board.IsWhiteToMove ? eval : -eval;
    }

    double EndGameWeight (Board board) {
        return Min(8.0 / board.GetAllPieceLists().Sum(x => x.Count), 1);
    }
    int ChebyshevDist (Square square) {
        return Max(CenterDist(square.Rank), CenterDist(square.File));
    }

    int CenterDist(int row) {
        return Max(3 - row, row - 4);
    }
    int Lerp(int a, int b, double t) {
        return (int) (a + (b - a) * t);
    }

}*/