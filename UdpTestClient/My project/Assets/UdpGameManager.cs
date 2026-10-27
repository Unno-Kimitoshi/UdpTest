using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class UdpGameManager : MonoBehaviour
{
    [Header("Network Settings")]
    public string serverIP = "192.168.0.151"; // スマホで遊ぶ場合はPCのローカルIPを入力
    public int serverPort = 4000;

    [Header("Game Objects")]
    public Transform myPlayer;       // 自分が操作するオブジェクト
    public Transform remotePlayer;   // 相手が動いたときに同期するオブジェクト

    private UdpClient udpClient;
    private Thread receiveThread;
    private bool isRunning = true;

    // スレッド間通信用のスナップショット
    private Vector3 latestRemotePosition;
    private bool hasNewPosition = false;

    void Start()
    {
        // ランダムなポートでUDPクライアントを初期化
        udpClient = new UdpClient(0); 

        // 最初の一歩として、サーバーにダミーデータを送って存在を知らせる（UDPの特性）
        SendData("HELLO");

        // 受信専用のスレッドを立ち上げる
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    void Update()
    {
        // 1. 自分の移動操作（矢印キーやWASD）
        float moveX = Input.GetAxis("Horizontal") * 5f * Time.deltaTime;
        float moveZ = Input.GetAxis("Vertical") * 5f * Time.deltaTime;
        myPlayer.Translate(moveX, 0, moveZ);

        // 2. 自分の最新座標をUDPで「確認応答なし」で送り続ける
        string posData = $"{myPlayer.position.x},{myPlayer.position.y},{myPlayer.position.z}";
        SendData(posData);

        // 3. 受信スレッドからデータが届いていたら、相手のオブジェクトの位置を更新
        if (hasNewPosition)
            {
            remotePlayer.position = latestRemotePosition;
            hasNewPosition = false;
        }
    }

    // サーバーへデータを送信（UDP垂れ流し）
    private void SendData(string message)
    {
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            udpClient.Send(data, data.Length, serverIP, serverPort);
        }
        catch (Exception e)
        {
            Debug.LogError($"Send Error: {e.Message}");
        }
    }

    // 【別スレッド】サーバーからのデータ受信を待ち受けるループ
    private void ReceiveData()
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

        while (isRunning)
        {
            try
            {
                // データが届くまでここでスレッドが待機（ブロッキング）する
                byte[] receivedBytes = udpClient.Receive(ref remoteEP);
                string message = Encoding.UTF8.GetString(receivedBytes);

                // 座標データ (x,y,z) のパース処理
                string[] coords = message.Split(',');
                if (coords.Length == 3)
                {
                    float x = float.Parse(coords[0]);
                    float y = float.Parse(coords[1]);
                    float z = float.Parse(coords[2]);

                    // Unityのコンポーネントはメインスレッドでしか触れないため、座標だけを一時保存
                    latestRemotePosition = new Vector3(x, y, z);
                    hasNewPosition = true;
                }
            }
            catch (Exception e)
            {
                if (isRunning) Debug.LogError($"Receive Error: {e.Message}");
            }
        }
    }

    void OnApplicationQuit()
    {
        isRunning = false;
        if (udpClient != null) udpClient.Close();
        if (receiveThread != null && receiveThread.IsAlive) receiveThread.Abort();
    }
}
