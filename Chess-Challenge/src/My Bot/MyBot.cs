﻿using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

public class MyBot : IChessBot {

    readonly int[] PieceValues = { 0, 100, 300, 300, 500, 900, 100000 };
    readonly int[] PawnPushValues = { 0, 5, 4, 10, 15, -5, 5, 0};

    bool isWhite;
    public Move Think(Board board, Timer timer) {
        Move bestMove = Move.NullMove;
        int evaluation = -2147483647;
        isWhite = board.IsWhiteToMove;

        foreach (Move move in board.GetLegalMoves()) {
            board.MakeMove(move);
            int newEval = -InvMax(board, 4, 0, -2147483647, -evaluation);
            board.UndoMove(move);
            if (newEval > evaluation) {
                evaluation = newEval;
                bestMove = move;
            }

        };
        Console.WriteLine((board.IsWhiteToMove ? 1 : -1) * evaluation);
        Console.WriteLine((board.IsWhiteToMove ? 1 : -1) * Evaluate(board));
        board.MakeMove(bestMove);
        Console.WriteLine((board.IsWhiteToMove ? 1 : -1) * Evaluate(board));
        Console.WriteLine();

        return bestMove;
    }

    int InvMax(Board board, int depth, int iterations, int alpha, int beta) {
    
        if (board.IsInCheckmate()) {
            return -2147483647;
        }

        if (board.IsDraw()) {
            return 0;
        }

        if (depth >= 0 || iterations >= 8) {
            return Evaluate(board);
        }

        Move[] legalMoves = OrderMoves(board, board.GetLegalMoves());

        foreach (Move move in legalMoves) {
            board.MakeMove(move);
            int newMove = -InvMax(board, depth - ((board.IsInCheck() || move.IsCapture || move.IsPromotion) ? 0 : 1), iterations + 1, -beta, -alpha);
            board.UndoMove(move);


            if (newMove >= beta) {
                return beta;
            }
            alpha = Math.Max(alpha, newMove);
        }

        return alpha;
    }

    Move[] OrderMoves(Board board, Move[] moves) {
        int[] moveScoreGuesses = new int[moves.Length];

        for (int i = 0; i < moves.Length; i++) {
            Move move = moves[i];
            int moveScoreGuess = moveScoreGuesses[i];
            board.MakeMove(move);

            if (board.IsInCheck()) {
                moveScoreGuess += 200; 
            }
            
            if (move.IsCapture) {
                moveScoreGuess += (PieceValues[(int)move.CapturePieceType] - PieceValues[(int)move.MovePieceType]);
            }

            if (move.IsPromotion) {
                moveScoreGuess += PieceValues[(int)move.PromotionPieceType];
            }
            board.UndoMove(move);

        }

        // Sort moves by moveScoreGuesses with Insertion Sort
        for (int i = 1; i < moves.Length; i++) {
            int j = i;
            while(j > 0 && moveScoreGuesses[i] < moveScoreGuesses[j - 1]) {
                moveScoreGuesses[j] = moveScoreGuesses[j - 1];
                moves[j] = moves[j - 1];
                j--;
            }
            moveScoreGuesses[j] = moveScoreGuesses[i];
            moves[j] = moves[i];
        }
        return moves;
    }
    int Evaluate(Board board) {
        if (board.IsInCheckmate()) {
            return -2147483647;
        }
        if (board.IsDraw()) {
            return 0;
        }

        int eval = 0;
        PieceList[] pieceLists = board.GetAllPieceLists();
        
        float endgameWeight = Math.Min(5 / pieceLists.Sum(x => x.Count), 1);
        
        for (int pieceType = 0; pieceType < PieceValues.Length - 1; pieceType++) {
            // Subtract when indexing piecelists because PieceValues includes "PieceType.None" and GetAllPieceLists() doesn't
            foreach(Piece piece in pieceLists[pieceType]) {
                eval += pieceEval(piece, endgameWeight);
            }
            foreach (Piece piece in pieceLists[pieceType + PieceValues.Length - 1]) {
                eval -= pieceEval(piece, endgameWeight);
            }
        }

        int whiteKingRank = board.GetKingSquare(true).Rank;
        int whiteKingFile = board.GetKingSquare(true).File;

        int blackKingRank = board.GetKingSquare(false).Rank;
        int blackKingFile = board.GetKingSquare(false).File;

        int cornerKingEval = (140 - 20 * Math.Max(Math.Abs(whiteKingRank - blackKingRank), Math.Abs(whiteKingFile - blackKingFile)));

        cornerKingEval += 10 * ((int)(Math.Abs(blackKingRank - 3.5) + Math.Abs(blackKingFile - 3.5)) + (int)(Math.Abs(whiteKingRank - 3.5) + Math.Abs(whiteKingFile - 3.5)));

        eval += (eval > 0 ? 1 : -1) * (int) (cornerKingEval * endgameWeight);
        return (board.IsWhiteToMove ? eval : -eval);
    }
    int pieceEval (Piece piece, float endgameWeight) {
        int eval = PieceValues[(int) piece.PieceType];
        Square square = piece.Square;
        if (piece.PieceType == PieceType.Pawn) {
            // Interpolate from a center pawn push to an even pawn push as the game progresses
            eval += (int) (5 * (piece.IsWhite ? (square.Rank - 1) : (6 - square.Rank)) * (endgameWeight + (1 - endgameWeight) * PawnPushValues[square.File]));
        }
        return eval;
    }

}