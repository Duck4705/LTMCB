using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine.SceneManagement;




public class PhotonGameManager : MonoBehaviourPunCallbacks
{
    [Header("UI Elements")]
    public Text statusText;
    public Button readyButton;
    public Button rotateButton;
    public Button quitButton;
    public Button nextButton;

    [Header("Grids")]
    public GameObject myGridParent;      // Grid của mình (hiện tàu của mình)
    public GameObject enemyGridParent;   // Grid của đối thủ (để bắn vào)

    [Header("Ships")]
    public GameObject[] ships;           // Tàu của mình
    private ShipScript shipScript;
    private int shipIndex = 0;

    [Header("Game Objects")]
    public GameObject missilePrefab;
    public GameObject firePrefab;
    public GameObject woodDock;

    // Game state variables
    private bool setupComplete = false;
    private bool playerReady = false;
    private bool opponentReady = false;
    private bool playerTurn = false;

    private List<GameObject> playerFires = new List<GameObject>();
    private List<GameObject> opponentFires = new List<GameObject>();

    private int opponentShipCount = 5;
    private int playerShipCount = 5;

    // Photon room properties keys
    private const string PLAYER_READY = "PlayerReady_";

    void Start()
    {
        // Kiểm tra tất cả các thành phần bắt buộc
        if (!ValidateComponents())
        {
            Debug.LogError("Missing required components! Check the Inspector.");
            return;
        }

        // Khởi tạo game
        if (ships.Length > 0)
        {
            shipScript = ships[shipIndex].GetComponent<ShipScript>();
        }

        // Setup UI buttons
        if (readyButton != null)
            readyButton.onClick.AddListener(OnReadyButtonClicked);

        if (rotateButton != null)
            rotateButton.onClick.AddListener(OnRotateButtonClicked);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitButtonClicked);

        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextButtonClicked);

        // Setup grids
        SetupGrids();

        // Update UI
        UpdateStatusText();
    }

    // Kiểm tra tất cả các thành phần cần thiết
    bool ValidateComponents()
    {
        if (statusText == null) { Debug.LogError("statusText is not assigned!"); return false; }
        if (readyButton == null) { Debug.LogError("readyButton is not assigned!"); return false; }
        if (rotateButton == null) { Debug.LogError("rotateButton is not assigned!"); return false; }
        if (quitButton == null) { Debug.LogError("quitButton is not assigned!"); return false; }
        if (nextButton == null) { Debug.LogError("nextButton is not assigned!"); return false; }
        if (myGridParent == null) { Debug.LogError("myGridParent is not assigned!"); return false; }
        if (enemyGridParent == null) { Debug.LogError("enemyGridParent is not assigned!"); return false; }
        if (ships == null || ships.Length == 0) { Debug.LogError("ships array is empty!"); return false; }
        if (missilePrefab == null) { Debug.LogError("missilePrefab is not assigned!"); return false; }
        if (firePrefab == null) { Debug.LogError("firePrefab is not assigned!"); return false; }

        return true;
    }

    // Thiết lập ban đầu cho hai grid
    void SetupGrids()
    {
        if (myGridParent == null || enemyGridParent == null)
        {
            Debug.LogError("Grid parents not assigned!");
            return;
        }

        // Trong setup, chỉ cho phép xếp tàu trên myGridParent
        ToggleGridInteraction(myGridParent, true);
        ToggleGridInteraction(enemyGridParent, false);

        // Ẩn tạm thời grid của đối thủ
        enemyGridParent.SetActive(false);
    }

    void ToggleGridInteraction(GameObject gridParent, bool enable)
    {
        if (gridParent == null) return;

        foreach (Transform child in gridParent.transform)
        {
            TileScript tileScript = child.GetComponent<TileScript>();
            if (tileScript != null)
            {
                tileScript.enabled = enable;
            }
        }
    }

    void OnNextButtonClicked()
    {
        if (setupComplete) return;

        if (shipScript == null)
        {
            Debug.LogError("shipScript is null!");
            return;
        }

        if (!shipScript.OnGameBoard())
        {
            shipScript.FlashColor(Color.red);
        }
        else
        {
            if (shipIndex < ships.Length - 1)
            {
                shipIndex++;
                shipScript = ships[shipIndex].GetComponent<ShipScript>();
                shipScript.FlashColor(Color.yellow);
            }
            else
            {
                // Đã đặt xong tất cả tàu, có thể bật nút Ready
                if (readyButton != null)
                    readyButton.interactable = true;
            }
        }
    }

    void UpdateStatusText()
    {
        if (statusText == null) return;

        if (!PhotonNetwork.IsConnected)
        {
            statusText.text = "Not connected to server!";
            return;
        }

        if (PhotonNetwork.CurrentRoom == null)
        {
            statusText.text = "Not in a room!";
            return;
        }

        if (PhotonNetwork.CurrentRoom.PlayerCount < 2)
        {
            statusText.text = "Waiting for another player to join...";
        }
        else if (!playerReady || !opponentReady)
        {
            statusText.text = "Place your ships and press Ready";
        }
        else if (playerTurn)
        {
            statusText.text = "Your turn - Select a tile to fire";
        }
        else
        {
            statusText.text = "Opponent's turn";
        }
    }

    void OnReadyButtonClicked()
    {
        if (setupComplete)
            return;

        if (!PhotonNetwork.IsConnected || PhotonNetwork.CurrentRoom == null)
        {
            Debug.LogError("Not connected to Photon!");
            statusText.text = "Connection error!";
            return;
        }

        // Kiểm tra xem tàu đã được đặt hợp lệ chưa
        foreach (GameObject ship in ships)
        {
            if (ship == null) continue;

            ShipScript ss = ship.GetComponent<ShipScript>();
            if (ss == null || !ss.OnGameBoard())
            {
                statusText.text = "Place all ships on board first!";
                return;
            }
        }

        playerReady = true;

        // Lưu trạng thái ready vào room properties
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
        props.Add(PLAYER_READY + PhotonNetwork.LocalPlayer.ActorNumber, true);
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);

        readyButton.interactable = false;
        rotateButton.interactable = false;

        statusText.text = "Waiting for opponent to be ready...";

        setupComplete = true;

        // Kiểm tra xem cả hai người chơi đã sẵn sàng chưa
        CheckBothPlayersReady();
    }

    void OnRotateButtonClicked()
    {
        if (setupComplete)
            return;

        if (shipScript != null)
        {
            shipScript.RotateShip();
        }
    }

    void OnQuitButtonClicked()
    {
        if (PhotonNetwork.IsConnected)
            PhotonNetwork.LeaveRoom();
        else
            SceneManager.LoadScene("Lobby");
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene("Lobby");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdateStatusText();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        statusText.text = "Opponent left the game";
        // Sau vài giây, quay về lobby
        Invoke("ReturnToLobby", 3.0f);
    }

    void ReturnToLobby()
    {
        if (PhotonNetwork.IsConnected)
            PhotonNetwork.LeaveRoom();
        else
            SceneManager.LoadScene("Lobby");
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        if (!PhotonNetwork.IsConnected || PhotonNetwork.CurrentRoom == null) return;

        // Kiểm tra nếu đối thủ đã sẵn sàng
        if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            foreach (Player player in PhotonNetwork.CurrentRoom.Players.Values)
            {
                if (player != null && player.ActorNumber != PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    object isReady;
                    if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(PLAYER_READY + player.ActorNumber, out isReady))
                    {
                        opponentReady = (bool)isReady;
                    }
                }
            }
        }

        CheckBothPlayersReady();
    }

    void CheckBothPlayersReady()
    {
        if (!PhotonNetwork.IsConnected || PhotonNetwork.CurrentRoom == null) return;

        if (playerReady && opponentReady && PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            // Bắt đầu game
            StartGame();
        }
    }

    void StartGame()
    {
        // Quyết định người đi trước (Master Client đi trước)
        playerTurn = PhotonNetwork.IsMasterClient;

        // Hiện cả 2 grid
        if (myGridParent != null) myGridParent.SetActive(true);
        if (enemyGridParent != null) enemyGridParent.SetActive(true);

        // Hiện tàu của mình
        foreach (GameObject ship in ships)
        {
            if (ship != null) ship.SetActive(true);
        }

        // Bật tắt tương tác với grid tùy thuộc vào lượt
        UpdateGridInteractions();

        UpdateStatusText();
    }

    // Cập nhật quyền tương tác với grid dựa vào lượt chơi
    void UpdateGridInteractions()
    {
        // Khi đến lượt, cho phép bắn vào grid đối phương
        ToggleGridInteraction(enemyGridParent, playerTurn);

        // Không cho tương tác với grid của mình trong game
        ToggleGridInteraction(myGridParent, false);
    }

    // Xử lý khi người chơi click vào tile
    public void TileClicked(GameObject tile)
    {
        if (tile == null) return;

        // Xác định tile thuộc grid nào
        bool isEnemyTile = IsEnemyTile(tile);

        if (!setupComplete)
        {
            // Đảm bảo chỉ đặt tàu trên grid của mình
            if (!isEnemyTile)
            {
                PlaceShip(tile);
                shipScript.SetClickedTile(tile);
            }
        }
        else if (playerTurn && isEnemyTile)
        {
            // Kiểm tra nếu tile đã bắn rồi thì không bắn nữa
            TileScript tileScript = tile.GetComponent<TileScript>();
            if (tileScript != null && tileScript.isShot)
            {
                return; // Không bắn lại ô đã bắn
            }

            // Bắn tàu trong giai đoạn chơi
            FireAtTile(tile);
        }
    }

    // Kiểm tra nếu tile thuộc grid đối phương
    bool IsEnemyTile(GameObject tile)
    {
        return enemyGridParent != null && tile.transform.IsChildOf(enemyGridParent.transform);
    }

    private void PlaceShip(GameObject tile)
    {
        if (shipIndex >= ships.Length || shipIndex < 0)
        {
            Debug.LogError("shipIndex out of range: " + shipIndex);
            return;
        }

        shipScript = ships[shipIndex].GetComponent<ShipScript>();
        if (shipScript == null)
        {
            Debug.LogError("ShipScript component not found on ship!");
            return;
        }

        shipScript.ClearTileList();
        Vector3 newVec = shipScript.GetOffsetVec(tile.transform.position);
        ships[shipIndex].transform.localPosition = newVec;
    }

    private void FireAtTile(GameObject tile)
    {
        if (!playerTurn || photonView == null) return;

        try
        {
            // Lấy thông tin vị trí của tile
            int tileNumber = GetTileNumber(tile);
            if (tileNumber < 0) return; // Lỗi trong GetTileNumber

            // Gửi lệnh bắn qua RPC
            photonView.RPC("RPC_FireAtTile", RpcTarget.All, tileNumber, PhotonNetwork.LocalPlayer.ActorNumber);

            // Chuyển lượt
            playerTurn = false;
            UpdateGridInteractions();
            UpdateStatusText();
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error in FireAtTile: " + e.Message);
        }
    }

    // Tìm tile theo số thứ tự trên grid cụ thể
    GameObject FindTileByNumber(GameObject gridParent, int tileNumber)
    {
        if (gridParent == null) return null;

        foreach (Transform child in gridParent.transform)
        {
            if (child.name.Contains("(" + tileNumber + ")"))
            {
                return child.gameObject;
            }
        }
        return null;
    }

    [PunRPC]
    void RPC_FireAtTile(int tileNumber, int shooterActorNumber)
    {
        try
        {
            GameObject tile = null;
            bool isMyTurn = shooterActorNumber == PhotonNetwork.LocalPlayer.ActorNumber;

            // Người bắn nhìn thấy hiệu ứng trên EnemyGrid, người bị bắn thấy trên MyGrid
            if (isMyTurn)
            {
                tile = FindTileByNumber(enemyGridParent, tileNumber);
            }
            else
            {
                tile = FindTileByNumber(myGridParent, tileNumber);
            }

            if (tile == null)
            {
                Debug.LogError("Tile not found: " + tileNumber);
                return;
            }

            // Tạo hiệu ứng tên lửa
            Vector3 tilePos = tile.transform.position;
            tilePos.y += 15;
            GameObject missile = Instantiate(missilePrefab, tilePos, missilePrefab.transform.rotation);

            // Gán script cho tên lửa để xử lý va chạm đúng
            if (isMyTurn)
            {
                if (missile.GetComponent<PhotonMissileScript>() == null)
                {
                    missile.AddComponent<PhotonMissileScript>();
                }
            }

            // Đánh dấu tile đã bắn
            TileScript tileScript = tile.GetComponent<TileScript>();
            if (tileScript != null)
            {
                tileScript.isShot = true;
            }

            // Kiểm tra va chạm với tàu (chỉ kiểm tra tàu của người bị bắn)
            if (!isMyTurn)
            {
                bool hitShip = false;
                ShipScript hitShipScript = null;

                foreach (GameObject ship in ships)
                {
                    if (ship == null) continue;

                    ShipScript shipScript = ship.GetComponent<ShipScript>();
                    if (shipScript != null && shipScript.ContainsTile(tile))
                    {
                        hitShip = true;
                        hitShipScript = shipScript;

                        // Đăng ký hit cho tàu này
                        shipScript.RegisterHit(tile);
                        bool sank = shipScript.HitCheckSank();

                        // Tạo hiệu ứng lửa
                        Vector3 firePos = tile.transform.position;
                        firePos.y += 0.2f;
                        GameObject fire = Instantiate(firePrefab, firePos, Quaternion.identity);
                        playerFires.Add(fire);

                        // Đổi màu tile khi trúng
                        if (tileScript != null)
                        {
                            tileScript.SetTileColor(1, new Color32(255, 0, 0, 255));
                            tileScript.SwitchColors(1);
                        }

                        // Báo kết quả cho người bắn
                        if (photonView != null)
                        {
                            photonView.RPC("RPC_HitResult", RpcTarget.Others, tileNumber, true, sank);
                        }

                        if (sank)
                        {
                            playerShipCount--;
                            if (playerShipCount <= 0 && photonView != null)
                            {
                                // Game over - thua
                                photonView.RPC("RPC_GameOver", RpcTarget.All, shooterActorNumber);
                            }
                        }
                        break;
                    }
                }

                if (!hitShip)
                {
                    // Thông báo trượt và đổi màu tile
                    if (tileScript != null)
                    {
                        tileScript.SetTileColor(1, new Color32(38, 57, 76, 255));
                        tileScript.SwitchColors(1);
                    }

                    // Báo kết quả cho người bắn
                    if (photonView != null)
                    {
                        photonView.RPC("RPC_HitResult", RpcTarget.Others, tileNumber, false, false);
                    }
                }

                // Chuyển lượt về cho mình sau khi đối thủ bắn
                playerTurn = true;
                UpdateGridInteractions();
                UpdateStatusText();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error in RPC_FireAtTile: " + e.Message);
        }
    }


    [PunRPC]
    void RPC_HitResult(int tileNumber, bool hit, bool sank)
    {
        try
        {
            // Tìm tile trong enemy grid (grid của đối thủ)
            GameObject tile = FindTileByNumber(enemyGridParent, tileNumber);
            if (tile != null)
            {
                TileScript tileScript = tile.GetComponent<TileScript>();
                if (tileScript != null)
                {
                    // Đổi màu tile dựa trên kết quả bắn
                    if (hit)
                    {
                        tileScript.SetTileColor(1, new Color32(255, 0, 0, 255));
                        statusText.text = sank ? "SUNK!" : "HIT!";

                        // Hiệu ứng lửa khi bắn trúng
                        if (sank && firePrefab != null)
                        {
                            Vector3 firePos = tile.transform.position;
                            firePos.y += 0.2f;
                            GameObject fire = Instantiate(firePrefab, firePos, Quaternion.identity);
                            opponentFires.Add(fire);
                        }
                    }
                    else
                    {
                        tileScript.SetTileColor(1, new Color32(38, 57, 76, 255));
                        statusText.text = "Miss!";
                    }

                    tileScript.SwitchColors(1);
                }
            }

            if (hit && sank)
            {
                opponentShipCount--;
                if (opponentShipCount <= 0)
                {
                    statusText.text = "YOU WIN!";
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error in RPC_HitResult: " + e.Message);
        }
    }

    [PunRPC]
    void RPC_GameOver(int winnerActorNumber)
    {
        try
        {
            bool isWinner = PhotonNetwork.LocalPlayer.ActorNumber == winnerActorNumber;

            if (isWinner)
                statusText.text = "YOU WIN!";
            else
                statusText.text = "YOU LOSE!";

            // Vô hiệu hóa tương tác với grid
            ToggleGridInteraction(myGridParent, false);
            ToggleGridInteraction(enemyGridParent, false);

            // Sau vài giây, quay về lobby
            Invoke("ReturnToLobby", 5.0f);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error in RPC_GameOver: " + e.Message);
        }
    }

    private int GetTileNumber(GameObject tile)
    {
        try
        {
            // Giả sử tên tile có dạng "Tile (X)" với X là số thứ tự
            string tileName = tile.name;

            if (!tileName.Contains("(") || !tileName.Contains(")"))
            {
                Debug.LogError("Invalid tile name format: " + tileName);
                return -1;
            }

            int startIndex = tileName.IndexOf('(') + 1;
            int endIndex = tileName.IndexOf(')');
            int length = endIndex - startIndex;

            if (length <= 0)
            {
                Debug.LogError("Invalid tile name format: " + tileName);
                return -1;
            }

            string numberStr = tileName.Substring(startIndex, length);

            int result;
            if (!int.TryParse(numberStr, out result))
            {
                Debug.LogError("Cannot parse number from tile name: " + tileName);
                return -1;
            }

            return result;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error in GetTileNumber: " + e.Message);
            return -1;
        }
    }
}
