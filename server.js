const dgram = require('dgram');
const server = dgram.createSocket('udp4');

const PORT = 4000;
const clients = new Map(); // 接続してきたクライアントの情報を保存 (Key: "IP:Port")

server.on('listening', () => {
    const address = server.address();
    console.log(`[Server] UDP Server listening on ${address.address}:${address.port}`);
});

server.on('message', (msg, rinfo) => {
    const clientKey = `${rinfo.address}:${rinfo.port}`;
    
    // 新しいクライアントなら登録（簡易的なハンドシェイクの代わり）
    if (!clients.has(clientKey)) {
        clients.set(clientKey, rinfo);
        console.log(`[Server] New client connected: ${clientKey}`);
    }

    // 届いたメッセージ（座標データなど）を、自分以外の全員に転送（UDP垂れ流し）
    for (let [key, client] of clients) {
        if (key !== clientKey) {
            server.send(msg, client.port, client.address, (err) => {
                if (err) console.error(`[Server] Send error to ${key}:`, err);
            });
        }
    }
});

server.bind(PORT);
