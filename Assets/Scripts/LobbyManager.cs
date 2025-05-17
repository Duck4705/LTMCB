using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;


public class LobbyManager : MonoBehaviourPunCallbacks
{
    public Button PlayWithBot;
    public Button PlayOnline;
    public Text connectionStatus;
    public GameObject loadingPanel;

    void Start()
    {
        if (PlayWithBot != null)
            PlayWithBot.onClick.AddListener(OnPlayWithBotClicked);

        if (PlayOnline != null)
            PlayOnline.onClick.AddListener(OnPlayOnlineClicked);

        // Ẩn loading panel khi vào lobby
        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        // Kết nối tới Photon khi mở game
        ConnectToPhoton();
    }

    void ConnectToPhoton()
    {
        if (connectionStatus != null)
            connectionStatus.text = "Connecting to server...";

        PhotonNetwork.ConnectUsingSettings();
    }

    void OnPlayWithBotClicked()
    {
        // Hiển thị loading nếu cần
        if (loadingPanel != null)
            loadingPanel.SetActive(true);

        SceneManager.LoadScene("PlayWithBot");
    }

    void OnPlayOnlineClicked()
    {
        if (!PhotonNetwork.IsConnected)
        {
            if (connectionStatus != null)
                connectionStatus.text = "Not connected to server. Reconnecting...";

            ConnectToPhoton();
            return;
        }

        // Hiển thị loading
        if (loadingPanel != null)
            loadingPanel.SetActive(true);

        if (connectionStatus != null)
            connectionStatus.text = "Joining a room...";

        // Tham gia phòng ngẫu nhiên
        PhotonNetwork.JoinRandomRoom();
    }

    // Khi kết nối thành công tới Photon Master Server
    public override void OnConnectedToMaster()
    {
        if (connectionStatus != null)
            connectionStatus.text = "Connected to server!";

        if (PlayOnline != null)
            PlayOnline.interactable = true;
    }

    // Khi không tìm thấy phòng
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        if (connectionStatus != null)
            connectionStatus.text = "Creating a new room...";

        // Tạo phòng mới
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 2 });
    }

    // Khi vào phòng thành công
    public override void OnJoinedRoom()
    {
        if (connectionStatus != null)
            connectionStatus.text = "Joined room: " + PhotonNetwork.CurrentRoom.Name;

        // Chuyển tới scene chơi online
        PhotonNetwork.LoadLevel("PlayWithPlayer");
    }

    // Hàm này xử lý lỗi kết nối
    public override void OnDisconnected(DisconnectCause cause)
    {
        if (connectionStatus != null)
            connectionStatus.text = "Disconnected: " + cause.ToString();

        if (PlayOnline != null)
            PlayOnline.interactable = false;

        // Thử kết nối lại sau vài giây
        Invoke("ConnectToPhoton", 3.0f);
    }
}
