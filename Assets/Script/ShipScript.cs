using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class ShipScript : MonoBehaviour
{
    List<GameObject> touchTiles = new List<GameObject>();
    public float xOffset = 0;
    public float zOffset = 0;
    private float nextZRotation = 90f;
    private GameObject clickedTile;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ClearTileList()
    {
        touchTiles.Clear();
    }
    public Vector3 GetOffsetVec(Vector3 tilePos)
    {
        return new Vector3(tilePos.x + xOffset,2,tilePos.z + zOffset);
    }
    public void RotateShip()
    {
        touchTiles.Clear();
        transform.localEulerAngles += new Vector3(0, 0, nextZRotation);
        nextZRotation *= -1;
        float temp = zOffset;
        zOffset = temp;
        SetPosition(clickedTile.transform.position);

    }

    public void SetPosition(Vector3 newVec)
    {
        transform.localPosition = new Vector3(newVec.x + xOffset, 2, newVec.z + zOffset);
    }
    public void SetClickedTile(GameObject tile)
    {
        clickedTile = tile;
    }
}
