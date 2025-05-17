using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TileScript : MonoBehaviour
{
    private GameManager gameManager;
    private PhotonGameManager photonGameManager;
    Ray ray;
    RaycastHit hit;

    private bool missileHit = false;
    Color32[] hitColor = new Color32[2];

    public bool isShot = false; // Đánh dấu tile đã bị bắn
    private bool isOnlineMode = false; // Xác định đang chơi chế độ nào

    void Start()
    {
        // Tìm các game manager
        gameManager = GameObject.FindObjectOfType<GameManager>();
        photonGameManager = GameObject.FindObjectOfType<PhotonGameManager>();

        // Xác định mode
        isOnlineMode = (photonGameManager != null);

        hitColor[0] = gameObject.GetComponent<MeshRenderer>().material.color;
        hitColor[1] = gameObject.GetComponent<MeshRenderer>().material.color;
    }

    void Update()
    {
        // Nếu đang nhấn UI (button), không xử lý click tile
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit))
        {
            if (Input.GetMouseButtonDown(0) && hit.collider.gameObject == gameObject)
            {
                // Xử lý khác nhau giữa online/offline
                if (isOnlineMode)
                {
                    if (!isShot && photonGameManager != null) // Chế độ online: kiểm tra isShot
                        photonGameManager.TileClicked(gameObject);
                }
                else
                {
                    if (!missileHit && gameManager != null) // Chế độ offline: dùng missileHit
                        gameManager.TileClicked(gameObject);
                }
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Missile"))
        {
            missileHit = true;
        }
        else if (collision.gameObject.CompareTag("EnemyMissile"))
        {
            hitColor[0] = new Color32(38, 57, 76, 255);
            GetComponent<Renderer>().material.color = hitColor[0];
        }
    }

    public void SetTileColor(int index, Color32 color)
    {
        hitColor[index] = color;
    }

    public void SwitchColors(int colorIndex)
    {
        GetComponent<Renderer>().material.color = hitColor[colorIndex];
    }

    // Phương thức reset công khai để có thể sử dụng khi restart game
    public void ResetTile()
    {
        missileHit = false;
        isShot = false;
        hitColor[0] = GetComponent<MeshRenderer>().material.color;
        hitColor[1] = GetComponent<MeshRenderer>().material.color;
    }
}
