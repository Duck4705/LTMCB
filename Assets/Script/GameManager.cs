using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
public class GameManager : MonoBehaviour
{
    public GameObject[] ships;

    [Header("HUD")]
    public Button nextBtn;
    public Button rotateBtn;

    private bool setupComplete = false;
    private bool playerTurn = true;
    private int shipIndex = 0;
    private ShipScript shipScript;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        shipScript = ships[shipIndex].GetComponent<ShipScript>();
        nextBtn.onClick.AddListener(() => NextShipClicked());
        rotateBtn.onClick.AddListener(() => RotateClicked());
    }
    private void NextShipClicked()
    {
        if(shipIndex <= ships.Length -2)
        {
            shipIndex++;
            shipScript = ships[shipIndex].GetComponent<ShipScript>();
            // shipScript.FlashColor(Color.yellow);
        }    
    }
    // Update is called once per frame
    void Update()
    {
        
    }
    public void TileClicked(GameObject tile)
    {
        if(setupComplete && playerTurn)
        {
            // drop a missile - BOOM

        } else if(!setupComplete)
        {
            PlaceShip(tile);
            shipScript.SetClickedTile(tile);
        }    
    }

    private void PlaceShip(GameObject tile)
    {
        shipScript = ships[shipIndex].GetComponent<ShipScript>();
        shipScript.ClearTileList();
        Vector3 newVec = shipScript.GetOffsetVec(tile.transform.position);
        ships[shipIndex].transform.localPosition = newVec;
    }

    void RotateClicked()
    {
        shipScript.RotateShip();
    }

    void SetShipClickedTile(GameObject tile)
    {
        shipScript.SetClickedTile(tile);
    }
}
