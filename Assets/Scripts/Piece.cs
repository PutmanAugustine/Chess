﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Piece : MonoBehaviour
{
    [SerializeField]
    private string _pieceType = "";
    [SerializeField]
    private bool _white = false;
    [SerializeField]
    private Global _global = null;
    [SerializeField]
    private FEN _fen = null;
    [SerializeField]
    private Tests _tests = null;
    [SerializeField]
    private Movement _movement = null;
    private bool _moving = false;
    private bool _enPassent = false;
    private bool _moved = false;
    private bool _kingside = false;
    private bool _lastTurnEnPassent = false;
    private GameObject _otherPawn = null;
    private List<GameObject> _rooks = new List<GameObject>();

    public void Start() { //Set all the private variables that can't be set in the piece prefabs
        GameObject[] tests = GameObject.FindGameObjectsWithTag("Script");
        foreach (GameObject test in tests) {
            _movement = test.GetComponent<Movement>();
            _fen = test.GetComponent<FEN>();
            _tests = test.GetComponent<Tests>();
        }
        tests = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject test in tests) {
            _global = test.GetComponent<Global>();
        }
        if (transform.position.x > 0) {
            _kingside = true;
        }
    }

    public void FixedUpdate() { //Move the piece forward if that's needed for an En Passent
        if (_lastTurnEnPassent) {
            if (_white) {
                transform.position = new Vector3(transform.position.x, transform.position.y + 9f, -1f);
            }
            else {
                transform.position = new Vector3(transform.position.x, transform.position.y - 9f, -1f);
            }
            Destroy(_otherPawn);
            _lastTurnEnPassent = false;
        }
    }

    public void SetMoved(bool newState) {
        _moved = newState;
    }

    public bool PassKingSide() { //Is the piece on the kingside at the start of the game
        return _kingside;
    }

    public void OnMouseDown() { //What happens when you click on a piece
        if (_tests.TestMoveCheck(transform.gameObject) & _global.PassTurn() == _white) {
            return;
        }
        PieceClicked(true, false);
    }

    public bool PieceClicked(bool move, bool overide = false, bool overide2 = false) { //What happens when you click on the piece, updates the variables related to the piece moving, threatens tiles and can change the FEN conditions if certain pieces are moved
        List<Collider2D> collisions = new List<Collider2D>();
        ContactFilter2D filters = new ContactFilter2D();
        filters.NoFilter();
        filters.useTriggers = true;
        bool legalMove = false;
        if (_global.PassTurn() == _white | overide) {
            if (!_moving | overide) {
                if (!overide) {
                    _global.UnCircle();
                    _moving = true;
                }
                if (move) {
                    _global.AssignMovingPiece(transform.gameObject);
                }
                switch (_pieceType) {
                    case "Queen":
                        legalMove = _movement.QueenMoveTest(transform.gameObject, move);
                        break;
                    case "King":
                        if (!overide | overide2) {
                            legalMove = _movement.KingMoveTest(transform.gameObject, move);
                        }
                        break;
                    case "Knight":
                        legalMove = _movement.KnightMoveTest(transform.gameObject, move);
                        break;
                    case "Pawn":
                        legalMove = _movement.PawnMoveTest(transform.gameObject, _moved, move);
                        break;
                    case "Bishop":
                        legalMove = _movement.BishopMoveTest(transform.gameObject, move);
                        break;
                    case "Rook":
                        legalMove = _movement.RookMoveTest(transform.gameObject, move);
                        break;
                }
            }
            else if (_moving & !overide) {
                _global.UnCircle();
                _moving = false;
            }
        }
        else {
            transform.gameObject.GetComponent<BoxCollider2D>().OverlapCollider(filters, collisions);
            foreach(Collider2D collision in collisions) {
                if (collision != null) {
                    if(collision.gameObject.CompareTag("Board")) {
                        if(collision.gameObject.GetComponent<Tile>().PassThreat((_global.PassMovingPiece()).gameObject)) {
                            collision.gameObject.GetComponent<Tile>().Movement(true);
                            _fen.ResetHalfmove();
                            break;
                        }
                    }
                }
            }
        }
        return legalMove;
    }

    public void TestKill() { //Remove an enemy piece when you take it
        List<Collider2D> collisions = new List<Collider2D>();
        ContactFilter2D filters = new ContactFilter2D();
        filters.NoFilter();
        filters.useTriggers = true;
        Physics2D.OverlapCollider(transform.gameObject.GetComponent<BoxCollider2D>(), filters, collisions);
        foreach (Collider2D other in collisions) {
            if (other != null) {
                if((other.CompareTag("Black") & _white) | (other.CompareTag("White") & !_white)) {
                    _global.UnThreaten(other.gameObject);
                    Destroy(other.gameObject);
                    break;
                }
            }
        }
    }
    
    public void Moved() {
        if (!_moved) {
            _enPassent = true;
        }
        else if (_moved) {
            _enPassent = false;
        }
        _moved = true;
        _moving = false;
    }

    public void SetEnPassentable(bool newState) {
        _enPassent = newState;
    }

    public bool PassColor() {
        return(_white);
    }

    public bool PassMoved() {
        return(_moved);
    }

    public string PassPiece() {
        return(_pieceType);
    }

    public bool EnPassent() {
        return(_enPassent);
    }

    public void NewTurn() {
        _enPassent = false;
    }

    public void EnPassented() {
        _lastTurnEnPassent = true;
    }

    public void SetEnPassent(GameObject pawn) {
        _otherPawn = pawn;
    }

    public void ChangeRook(GameObject rook, bool add, bool clear = false) {
        if (add) {
            _rooks.Add(rook);
        }
        else if (!add) {
            _rooks.Remove(rook);
        }
        if (clear) {
            _rooks.Clear();
        }
    }

    public List<GameObject> PassRooks() {
        return(_rooks);
    }
}
