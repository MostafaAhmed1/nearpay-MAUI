# NearPay Payment Plugin SDK — Checklist (Official)

> الهدف: تشغيل الدفع (Card / NFC) عبر **Payment Plugin SDK** (`nearpay-sdk`) حسب وثائق NearPay الرسمية.

## A) ما تم ضبطه داخل مشروعنا (تم تنفيذها بالفعل)

1) **Package Name**
- `com.alexsoftcode.alexpos`

2) **SDK dependency**
- `io.nearpay:nearpay-sdk:2.1.98`
- ومعها shared modules `2.1.98`

3) **AndroidManifest**
- `tools:replace="android:allowBackup"`
- الصلاحيات المطلوبة:
  - `ACCESS_FINE_LOCATION`
  - `ACCESS_NETWORK_STATE`
  - `INTERNET`
  - `READ_PHONE_STATE`
  - `NFC`

## B) متطلبات يجب أن توفرها أنت (لا يمكن تنفيذها من داخل الكود)

### 1) NearPay Dashboard (Sandbox)
1. إنشاء/تفعيل حساب Sandbox عبر NearPay (يحتاج البريد + الهاتف)
2. تسجيل الـ **package name** في:
   - Dashboard → Apps → Add App → package name = `com.alexsoftcode.alexpos`

### 2) Terminal
1. Dashboard → Terminals → Create terminal
2. احصل على **Tid**
3. (إن كنت تستخدم UserEnter / Email / Mobile) يجب **Invite user** لهذا الـ terminal ويقبل الدعوة

### 3) JWT (إن كنت تستخدم JWT)
1. Dashboard → Credentials:
   - **Client Key** (client_uuid)
   - **JWT Key** → Generate لتحميل `pos_key.pem` (مفتاح خاص لتوقيع JWT)
2. توليد JWT بتوقيع RS256 (حسب مثال NearPay)
3. الـ payload يجب أن يحتوي على:
   - `ops: "auth"`
   - `terminal_id: "<Tid>"`
   - `client_uuid: "<Client Key>"`

> لا تشارك `pos_key.pem` ولا كلمات مرور keystore مع أي شخص.

### 4) جهاز الاختبار
- جهاز Android فعلي يدعم NFC لتجربة الدفع.

## C) التسلسل الرسمي داخل التطبيق (مختصر)

1) Initialize (إنشاء NearPay instance)
2) Setup (اختياري — سيقوم بتثبيت Payment Plugin وتسجيل الدخول إن لزم)
3) Purchase (تشغيل عملية شراء)

## مصادر NearPay الرسمية
- Payment Plugin SDK: https://docs.nearpay.io/sa/en/guides/sdk
- Preparing your App: https://docs.nearpay.io/sa/en/guides/preparing-your-app
- Quick Start (Plugin): https://docs.nearpay.io/sa/en/guides/quick-start-plugin

