/**
 * توليد JWT لـ NearPay من ملف pos_key.pem
 * الاستخدام:
 *   npm i jsonwebtoken
 *   node generate-jwt.js <path-to-pos_key.pem> <terminal_id> <client_uuid_or_merchant_uuid> [client|merchant]
 *
 * مثال:
 *   node generate-jwt.js ./pos_key.pem PS239210 00000000-0000-0000-0000-000000000000 client
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

