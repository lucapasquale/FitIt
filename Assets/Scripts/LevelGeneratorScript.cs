﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LevelGeneratorScript : MonoBehaviour
{
    int gridSize { get { return SpawnScript.Instance.gridSize; } }
    int blockSize { get { return SpawnScript.Instance.blockSize; } }
    GameObject[,] gridGO { get { return GridScript.Instance.gridGO; } set { GridScript.Instance.gridGO = value; } }

    public List<Vector2> filledListPos = new List<Vector2>();
    public List<GameObject> blocksUsed = new List<GameObject>();
    Vector2 gridOffset;


    public void Click1()
    {

        for (int n = 0; n < 100; n++)
        {
            PlaceRandomBlocks();
        }

        for (int n = 0; n < 2; n++)
            for (int i = 0; i <= gridSize - blockSize; i++)
                for (int j = 0; j <= gridSize - blockSize; j++)
                    CompleteGrid(i, j);

        FillEmptyGrid();
        
    }

    public void Click2()
    {
        BlockScript[] blocks = GameObject.FindObjectsOfType<BlockScript>();

        foreach (BlockScript block in blocks)
        {
            var children = new List<GameObject>();
            foreach (Transform child in block.transform) children.Add(child.gameObject);
            children.ForEach(child => Destroy(child));
            block.bPlaced = false;
            block.bPos = Vector2.zero;
            blocksUsed.Add(block.gameObject);
        }
    }

    #region Private
    //Tenta colocar blocos de 4 ou 5 squares em uma posição aleatoria no grid
    void PlaceRandomBlocks()
    {
        Vector2 ponto = new Vector2(Random.Range(0, (gridSize - blockSize) + 1), Random.Range(0, (gridSize - blockSize) + 1));

        int squaresNumber = Random.Range(4, 6);
        GameObject randomBlock = SpawnScript.Instance.blocksList[squaresNumber][Random.Range(0, SpawnScript.Instance.blocksList[squaresNumber].Count)] as GameObject;
        GameObject block = Instantiate(randomBlock, SpawnScript.Instance.spawnLocations[SpawnScript.Instance.activeBlocksNumber].transform.position, Quaternion.identity) as GameObject;

        for (int rot = 0; rot < Random.Range(0, 5); rot++)
            block.GetComponent<BlockScript>().RotateMatrix(true);

        if (CheckPosition(block, ponto))
        {
            block.transform.parent = GameObject.Find("Blocks").transform;
            MoveBlockGrid(block, ponto);
            Debug.Log(block + "posicionado em " + ponto.x + " " + ponto.y);
        }
        else
        {
            SpawnScript.Instance.activeBlocksNumber--;
            Destroy(block);
        }
    }

    //Cria string de um local 3x3 do grid. 1 = livre, 0 = ocupado
    string GridLocID(int posX, int posY)
    {
        string blockID;
        int bitPos = 0;
        int bID = 0;

        for (int y = posY; y < posY + blockSize; y++)
        {
            for (int x = posX + (blockSize - 1); x >= posX; x--)
            {
                if (gridGO[x, y].GetComponent<GridTile>().gType == GridType.Empty)
                {
                    bID += (int)Mathf.Pow(2, bitPos);
                    gridOffset = new Vector2(x, y);
                }
                bitPos++;
            }
        }
        blockID = System.Convert.ToString(bID, 2);
        char[] removeChars = { '0' };
        return blockID.TrimEnd(removeChars);
    }

    //Checa todos os espaços 3x3 do grid e tenta preencher com um bloco
    void CompleteGrid(int i, int j)
    {
        bool teste = false;
        string gridSpace = GridLocID(i, j);
        int nBlocks = gridSpace.Split('1').Length - 1;

        if (nBlocks >= 2 && nBlocks <= 5)
        {
            foreach (GameObject block in SpawnScript.Instance.blocksList[nBlocks])
            {
                GameObject blockGO = Instantiate(block, SpawnScript.Instance.spawnLocations[SpawnScript.Instance.activeBlocksNumber].transform.position, Quaternion.identity) as GameObject;
                for (int r = 0; r < 4; r++)
                {
                    if (gridSpace == blockGO.GetComponent<BlockScript>().rotationIDs[r])
                    {
                        //Roda o bloco até estar na posição certa
                        for (int rotations = r; rotations > 0; rotations--)
                            blockGO.GetComponent<BlockScript>().RotateMatrix(true);

                        BlockScript bs = blockGO.GetComponent<BlockScript>();

                        if (CheckPosition(blockGO, gridOffset - bs.bOffsets[r]))
                        {
                            blockGO.transform.parent = GameObject.Find("Blocks").transform;
                            MoveBlockGrid(blockGO, gridOffset - bs.bOffsets[r]);
                            Debug.Log(blockGO + "completado em " + (new Vector2(i, j)).ToString());
                            teste = true;
                        }
                    }
                }
                if (!teste)
                {
                    SpawnScript.Instance.activeBlocksNumber--;
                    Destroy(blockGO);
                }
                teste = false;
            }
        }
    }

    //Preenche blocos restantes com GridFilled
    void FillEmptyGrid()
    {
        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                if (GridScript.Instance.gridGO[i, j].GetComponent<GridTile>().gType == GridType.Empty)
                {
                    GridScript.Instance.filledListPos.Add(new Vector2(i, j));
                    GridScript.Instance.FillGrid(new Vector2(i, j));
                }

            }
        }
        filledListPos = GridScript.Instance.filledListPos;
}

    bool CheckPosition(GameObject blockGO, Vector2 destiny)
    {

        foreach (BlockTile square in blockGO.GetComponent<BlockScript>().tileList)
        {
            //Bloco esta dentro do grid?
            Vector2 squarePos = destiny + square.relativePos;
            if (squarePos.x < 0 || squarePos.x >= SpawnScript.Instance.gridSize || squarePos.y < 0 || squarePos.y >= SpawnScript.Instance.gridSize)
            {
                Debug.Log("Bloco fora do grid!");
                return false;
            }

            //Existem outros blocos la?
            if (GridScript.Instance.gridGO[(int)squarePos.x, (int)squarePos.y].GetComponent<GridTile>().gType != GridType.Empty)
            {
                //Debug.Log("Não pode ser colocado " + blockGO + " em " + destiny.x + "," + destiny.y + " pois ja existe um bloco la");
                blockGO.GetComponent<BlockScript>().bPlaced = false;
                return false;
            }
        }
        return true;
    }

    void MoveBlockGrid(GameObject blockGO, Vector2 destiny)
    {
        Color color = GridScript.Instance.blocksColor[SpawnScript.Instance.activeBlocksNumber - 1];
        foreach (BlockTile square in blockGO.GetComponent<BlockScript>().tileList)
        {
            Vector2 pos = destiny + square.relativePos;
            GridTile gridSquare = GridScript.Instance.gridGO[(int)pos.x, (int)pos.y].GetComponent<GridTile>();
            gridSquare.gType = GridType.Used;
            gridSquare.gridPos = pos;
            gridSquare.parentBlock = blockGO.GetComponent<BlockScript>();
            gridSquare.bNumber = gridSquare.parentBlock.bNumber;
            gridSquare.GetComponent<SpriteRenderer>().color = color;
            gridSquare.GetComponent<SpriteRenderer>().sortingLayerName = "block";
            blockGO.GetComponent<BlockScript>().bPlaced = true;
            blockGO.GetComponent<BlockScript>().bPos = destiny;

            square.transform.localPosition = new Vector3(square.relativePos.x, square.relativePos.y, -1);
        }
        blockGO.transform.position = SpawnScript.Instance.spawnLocations[blockGO.GetComponent<BlockScript>().bNumber - 1].transform.position - Vector3.forward;

    }
    #endregion
}
