using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipScript : MonoBehaviour
{
    public float xOffset = 0;
    public float zOffset = 0;
    private float nextZRotation = 90f;
    private GameObject clickedTile;
    private int hitCount = 0;
    public int shipSize;

    private Material[] allMaterials;

    public List<GameObject> touchTiles = new List<GameObject>(); // Chuyển thành public
    public List<GameObject> hitTiles = new List<GameObject>(); // Thêm danh sách các ô đã bị bắn

    List<Color> allColors = new List<Color>();

    private void Start()
    {
        allMaterials = GetComponent<Renderer>().materials;
        for (int i = 0; i < allMaterials.Length; i++)
            allColors.Add(allMaterials[i].color);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Tile"))
        {
            // Đảm bảo không thêm trùng tile
            if (!touchTiles.Contains(collision.gameObject))
            {
                touchTiles.Add(collision.gameObject);
            }
        }
    }

    public void ClearTileList()
    {
        touchTiles.Clear();
    }

    public Vector3 GetOffsetVec(Vector3 tilePos)
    {
        return new Vector3(tilePos.x + xOffset, 2, tilePos.z + zOffset);
    }

    public void RotateShip()
    {
        if (clickedTile == null) return;
        touchTiles.Clear();
        transform.localEulerAngles += new Vector3(0, 0, nextZRotation);
        nextZRotation *= -1;
        float temp = xOffset;
        xOffset = zOffset;
        zOffset = temp;
        SetPosition(clickedTile.transform.position);
    }

    public void SetPosition(Vector3 newVec)
    {
        ClearTileList();
        transform.localPosition = new Vector3(newVec.x + xOffset, 2, newVec.z + zOffset);
    }

    public void SetClickedTile(GameObject tile)
    {
        clickedTile = tile;
    }

    public bool OnGameBoard()
    {
        return touchTiles.Count == shipSize;
    }

    // Phương thức mới - kiểm tra xem một tile có thuộc tàu không
    public bool ContainsTile(GameObject tile)
    {
        return touchTiles.Contains(tile);
    }

    // Phương thức mới - đăng ký hit cho tàu
    public bool RegisterHit(GameObject tile)
    {
        // Kiểm tra tile có thuộc tàu không và chưa bị bắn trúng
        if (ContainsTile(tile) && !hitTiles.Contains(tile))
        {
            hitTiles.Add(tile);
            hitCount++;
            return true;
        }
        return false;
    }

    public bool HitCheckSank()
    {
        hitCount++;
        return shipSize <= hitCount;
    }

    // Phương thức mới - lấy số lượng tile tàu đang đứng
    public int GetTouchTilesCount()
    {
        return touchTiles.Count;
    }

    public void FlashColor(Color tempColor)
    {
        foreach (Material mat in allMaterials)
        {
            mat.color = tempColor;
        }
        Invoke("ResetColor", 0.5f);
    }

    // Reset lại tàu khi restart game
    public void ResetShip()
    {
        hitCount = 0;
        hitTiles.Clear();
    }

    private void ResetColor()
    {
        int i = 0;
        foreach (Material mat in allMaterials)
        {
            mat.color = allColors[i++];
        }
    }
}
