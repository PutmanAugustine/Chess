﻿using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class Tests : MonoBehaviour //All the tests used by pieces and tiles throughout a game
{
    [SerializeField]
    private Global _global = null;
    [SerializeField]
    private Movement _movement = null;
    [SerializeField]
    private Checkmate _checkmate = null;
    [SerializeField]
    private GameObject[] _kings = {null, null};
    private bool _delayCheckmate = false;

    private void Start() {
        GameObject[] kings = GameObject.FindGameObjectsWithTag("White");
        foreach (GameObject king in kings) {
            if (king.GetComponent<Piece>().PassPiece() == "King") {
                _kings[0] = king;
                break;
            }
        }
        kings = GameObject.FindGameObjectsWithTag("Black");
        foreach (GameObject king in kings) {
            if (king.GetComponent<Piece>().PassPiece() == "King") {
                _kings[1] = king;
                break;
            }
        }
    }

    private void FixedUpdate() { //The tests that occur every frame, such as check and blockable tiles
        if (!_delayCheckmate) {
            TestCheck(false);
        }
        else {
            _delayCheckmate = false;
        }
        if (_checkmate.PassCheck()) {
            if (_global.PassTurn()) {
                MarkBlockableTiles(_kings[0]);
            }
            else {
                MarkBlockableTiles(_kings[1]);
            }
        }
        else {
            GameObject[] tiles = GameObject.FindGameObjectsWithTag("Board");
            foreach (GameObject tile in tiles) {
                tile.GetComponent<Tile>().CanBlock(false);
            }
        }
    }

    public void DelayCheckmate() { //Delay the checkmate, to avoid bugs
        _delayCheckmate = true;
    }

    public bool TestMoveCheck(GameObject target) { //Will moving the passed piece put their king in check?
        HashSet<GameObject> threats = new HashSet<GameObject>();
        List<Collider2D> collisions = new List<Collider2D>();
        ContactFilter2D filters = new ContactFilter2D();
        filters.NoFilter();
        Vector3 fromPosition = target.transform.position;
        Vector3 toPosition = new Vector3(0f,0f,0f);
        Vector3 direction = toPosition;
        filters.useTriggers = true;
        target.GetComponent<BoxCollider2D>().OverlapCollider(filters, collisions);
        foreach (Collider2D tile in collisions) {
            if (tile) {
                if (tile.transform.gameObject.CompareTag("Board")) {
                    threats = tile.transform.gameObject.GetComponent<Tile>().PassThreats();
                    foreach (GameObject threat in threats) {
                        if (!threat) {
                            continue;
                        }
                        if (threat == target) {
                            continue;
                        }
                        if (threat.tag == target.tag) {
                            continue;
                        }
                        if (threat.GetComponent<Piece>().PassPiece() != "Knight" & threat.GetComponent<Piece>().PassPiece() != "King" & threat.GetComponent<Piece>().PassPiece() != "Pawn") {
                            toPosition = threat.transform.position;
                            direction = fromPosition - toPosition;
                            RaycastHit2D[] hitArray = Physics2D.RaycastAll(new Vector2(target.transform.position.x, target.transform.position.y), direction, -Mathf.Infinity);
                            foreach (RaycastHit2D hit in hitArray) {
                                if (hit) {
                                    if (!hit.transform.gameObject.CompareTag("Board")) {
                                        if (hit.transform.gameObject == target) {
                                            continue;
                                        }
                                        if (hit.transform.gameObject.GetComponent<Piece>().PassPiece() == "King") {
                                            return true;
                                        }
                                        else {
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        return false;
    }

    public HashSet<GameObject> TestCheck(bool dummy = false) { //Check for check
        HashSet<GameObject> threats = new HashSet<GameObject>();
        HashSet<GameObject> tiles = new HashSet<GameObject>();
        List<Collider2D> kingTiles = new List<Collider2D>();
        bool check = false;
        GameObject king = null;
        bool safe = true;
        List<Collider2D> collisions = new List<Collider2D>();
        ContactFilter2D filters = new ContactFilter2D();
        string[] tag = {"", ""};
        if (_global.PassTurn()) {
            tag[0] = "White";
            tag[1] = "Black";
        }
        else {
            tag[1] = "White";
            tag[0] = "Black";
        }
        GameObject[] pieces = GameObject.FindGameObjectsWithTag(tag[1]);
        Array.Resize(ref pieces, 24);
        filters.NoFilter();
        filters.useTriggers = true;
        foreach (GameObject piece in pieces) {
            if (piece) {
                piece.GetComponent<Piece>().PieceClicked(false, true);
            }
        }
        pieces = GameObject.FindGameObjectsWithTag(tag[0]);
        foreach (GameObject piece in pieces) {
            if (piece) {
                if (piece.GetComponent<Piece>().PassPiece() == "King") {
                    king = piece;
                    piece.GetComponent<BoxCollider2D>().OverlapCollider(filters, kingTiles);
                    foreach (Collider2D kingTile in kingTiles) {
                        if (kingTile.transform.gameObject.CompareTag("Board")) {
                            threats = kingTile.transform.gameObject.GetComponent<Tile>().PassThreats();
                            foreach (GameObject threat in threats) {
                                if (threat) {
                                    if (threat.CompareTag(tag[1]) == piece.CompareTag(tag[0]) & piece.transform.position != threat.transform.position) {
                                        tiles.Add(kingTile.transform.gameObject);
                                        check = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        if (check & !dummy) {
            int i = 0;
            GameObject[] tilesTest = GameObject.FindGameObjectsWithTag("Board");
            foreach (GameObject tile in tilesTest) {
                if (tile.GetComponent<Tile>().Blockable()) {
                    if (new Vector2(tile.transform.gameObject.transform.position.x, tile.transform.gameObject.transform.position.y) != new Vector2(king.transform.position.x, king.transform.position.y)) {
                        threats = tile.transform.gameObject.GetComponent<Tile>().PassThreats();
                        foreach (GameObject threat in threats) {
                            if (threat.tag != king.tag) {
                                safe = false;
                                break;
                            }
                        }
                        if (safe) {
                            i++;
                        }
                        safe = true;
                    }
                }
            }
            if (i > 0 | _movement.KingMoveTest(king, false)) {
                _checkmate.EnterCheck();
            }
            else {
                _checkmate.EnterCheckmate(!_global.PassTurn());
            }
        }
        else if(!check & !dummy) {
            safe = false;
            _checkmate.ExitCheck();
            foreach (GameObject piece in pieces) {
                if (piece.GetComponent<Piece>().PieceClicked(false, true, true)) {
                    safe = true;
                }
            }
            if (!safe) {
                _checkmate.EnterStalemate("No Legal Moves!");
            }
            //else {
                //_checkmate.EnterStalemate("No Legal Moves!");
            //}
        }
        return (tiles);
    }

    public void MarkBlockableTiles(GameObject targetOb) { //Mark which tiles can be blocked to remove the king from check
        List<Collider2D> tilesCovered = new List<Collider2D>();
        HashSet<GameObject> threats = new HashSet<GameObject>();
        ContactFilter2D filters = new ContactFilter2D();
        Vector3 fromPosition = targetOb.transform.position;
        Vector3 toPosition = new Vector3(0f,0f,0f);
        Vector3 direction = toPosition;
        filters.NoFilter();
        bool skip = false;
        filters.useTriggers = true;
        targetOb.GetComponent<BoxCollider2D>().OverlapCollider(filters, tilesCovered);
        foreach (Collider2D tile in tilesCovered) {
            if (tile.transform.gameObject != null & tile.tag == "Board") {
                threats = tile.transform.gameObject.GetComponent<Tile>().PassThreats();
                foreach (GameObject threat in threats) {
                    if (threat) {
                        if (threat.tag != targetOb.tag) {
                            toPosition = threat.transform.position;
                            if (threat.GetComponent<Piece>().PassPiece() == "Knight") {
                                fromPosition = threat.transform.position;
                                skip = true;
                            }
                            direction = toPosition - fromPosition;
                            RaycastHit2D[] hitArray = Physics2D.RaycastAll(new Vector2(fromPosition.x, fromPosition.y), direction, Mathf.Sqrt(Mathf.Pow(fromPosition.x-toPosition.x, 2) + Mathf.Pow(fromPosition.y-toPosition.y, 2)));
                            foreach (RaycastHit2D hit in hitArray) {
                                if (!skip) {
                                    skip = true;
                                    continue;
                                }
                                if (hit.transform.gameObject.CompareTag("Board")) {
                                    hit.transform.gameObject.GetComponent<Tile>().CanBlock(true);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
