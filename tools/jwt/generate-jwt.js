/**
 * توليد JWT لـ NearPay من ملف pos_key.pem
 * الاستخدام:
 *   npm i jsonwebtoken
 *   node generate-jwt.js <path-to-pos_key.pem> <terminal_id> <client_uuid_or_merchant_uuid> [client|merchant]
 *
 * مثال:
 * npm init -y
 * npm i jsonwebtoken
 * node .\generate-jwt.js .\1780363559467-private-key.pem 0211806200118062 19725176-9d42-4ceb-b808-695316784da8 client
 *
 *
 * 
 *   node generate-jwt.js ./pos_key.pem PS239210 00000000-0000-0000-0000-000000000000 client
 *
 * 
 * eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJkYXRhIjp7Im9wcyI6ImF1dGgiLCJ0ZXJtaW5hbF9pZCI6IjAyMTE4MDYyMDAxMTgwNjIiLCJjbGllbnRfdXVpZCI6IjQ4NTdlZTM4LThlZWQtNDc1YS04N2Q2LWQ4ZjFlOGUzZjNhMyJ9LCJpYXQiOjE3ODAzNzg3NDd9.MKJIj5RU3hAbmwA3-vyxc9cYbPEsZXkp3WVsPh6KMukJg0SDnh9JWgBJCQvjpV_FZlleyXojUN-duGEC19lxzynGSFbNuKu0uJKjVrdAr41N8EeCZrdq_wpck53qlB_jSMheAI3I6bXsG0DlfRU3-sw7el-M8zQiaVwjIcqfCKCOuNn3_XUFjFlfYL1tscdwBFOK0d0fwV1mbiQufAUih5FzL6OkGczSbpZ9afSnaH5iPdPJLOmOMUh8Yt78Cap0Y51JhHWNsawdjKp4YrrV0tQFyGzT8I5I6VS-mnnYR27L-xnSoHHNoFlPOMeDHAgY51WT698vzaNOfi7eK3TBkw
 */

const fs = require("fs");
const jwt = require("jsonwebtoken");

const pemPath = process.argv[2];
const terminalId = process.argv[3];
const uuid = process.argv[4];
const mode = (process.argv[5] || "client").toLowerCase();

if (!pemPath || !terminalId || !uuid) {
  console.error("Usage: node generate-jwt.js <pemPath> <terminal_id> <client_uuid_or_merchant_uuid> [client|merchant]");
  process.exit(1);
}

const privateKey = fs.readFileSync(pemPath);

const payload = {
  data: {
    ops: "auth",
    terminal_id: terminalId,
    ...(mode === "merchant" ? { merchant_uuid: uuid } : { client_uuid: uuid }),
  },
};

const token = jwt.sign(payload, privateKey, { algorithm: "RS256" });
console.log(token);

