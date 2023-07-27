/*using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

public class MyBot : IChessBot {

    readonly int[] PieceValues = { 0, 100, 300, 300, 500, 900, 100000 };

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

    int InvMax(Board board, int depth, int iterations, int alpha, int beta, bool captures = false) {
    
        if (board.IsInCheckmate()) {
            return -2147483647;
        }

        if (board.IsDraw()) {
            return 0;
        }

        if (depth == 0 || iterations >= 8) {
            captures = true;
        }

        Move[] legalMoves = OrderMoves(board, board.GetLegalMoves(captures));

        if (legalMoves.Length == 0) {
            return Evaluate(board);
        }

        foreach (Move move in legalMoves) {
            board.MakeMove(move);
            int newMove = -InvMax(board, depth - ((board.IsInCheck() || move.IsCapture || move.IsPromotion) ? 0 : 1), iterations + 1, -beta, -alpha, captures);
            if (board.IsInCheck()) { newMove += 20; };
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
            board.MakeMove(move);

            if (board.IsInCheck()) {
                moveScoreGuesses[i] += 200; 
            }
            
            if (move.IsCapture) {
                moveScoreGuesses[i] += (PieceValues[(int)move.CapturePieceType] - PieceValues[(int)move.MovePieceType]);
            }

            if (move.IsPromotion) {
                moveScoreGuesses[i] += PieceValues[(int)move.PromotionPieceType];
            }
            board.UndoMove(move);

        }

        for (int i = 1; i < moves.Length; i++) {
            int key = moveScoreGuesses[i];
            Move move = moves[i];
            int j = i;
            while(j > 0 && key < moveScoreGuesses[j - 1]) {
                moveScoreGuesses[j] = moveScoreGuesses[j - 1];
                moves[j] = moves[j - 1];
                j--;
            }
            moveScoreGuesses[j] = key;
            moves[j] = move;
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
        
        for (int pieceType = 0; pieceType < PieceValues.Length - 1; pieceType++) {
            // Subtract when indexing piecelists because PieceValues includes "PieceType.None" and GetAllPieceLists() doesn't
            foreach(Piece piece in pieceLists[pieceType]) {
                eval += pieceEval(piece);
            }
            foreach (Piece piece in pieceLists[pieceType + PieceValues.Length - 1]) {
                eval -= pieceEval(piece);
            }
        }

        int whiteKingRank = board.GetKingSquare(true).Rank;
        int whiteKingFile = board.GetKingSquare(true).File;

        int blackKingRank = board.GetKingSquare(false).Rank;
        int blackKingFile = board.GetKingSquare(false).File;

        int cornerKingEval = (140 - 20 * Math.Max(Math.Abs(whiteKingRank - blackKingRank), Math.Abs(whiteKingFile - blackKingFile)));

        cornerKingEval += 10 * ((int)(Math.Abs(blackKingRank - 3.5) + Math.Abs(blackKingFile - 3.5)) + (int)(Math.Abs(whiteKingRank - 3.5) + Math.Abs(whiteKingFile - 3.5)));

        eval += (eval > 0 ? 1 : -1) * cornerKingEval * board.PlyCount / 100;
        return (board.IsWhiteToMove ? eval : -eval);
    }
    int pieceEval (Piece piece) {
        int eval = PieceValues[(int) piece.PieceType];
        if (piece.PieceType == PieceType.Pawn) {
            eval += 5 * (piece.IsWhite ? (piece.Square.Rank - 1) : (6 - piece.Square.Rank));
        }
        return eval;
    }

}*/