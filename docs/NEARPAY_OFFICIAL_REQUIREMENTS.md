# متطلبات NearPay الرسمية (مختصر + روابط)

> هذا المستند يجمع المتطلبات **كما وردت في وثائق NearPay الرسمية** فقط، بدون افتراضات إضافية.

## 1) متطلبات اختبار الدفع (Card/NFC) محلياً

- فريق NearPay يحتاج إنشاء حساب Sandbox لك باستخدام البريد/الهاتف + **Android package name** لبدء عملية الربط.  
- يلزم جهاز Android فعلي يدعم NFC لتجربة الدفع وتشغيل التطبيق عليه.

## 2) Secure Maven Repository / Private Token

في الـ Quick Start توضح NearPay أنه يلزم إضافة Maven repository خاص على GitLab مع `Private-Token` لتحميل dependencies الخاصة بالـ SDK.

> ملاحظة أمنية: لا تضع الـ token داخل GitHub. خزنّه محلياً (Environment Variable) أو في ملف محلي غير مرفوع.

## 3) Dependencies المذكورة في Quick Start

تذكر الوثائق إضافة dependency للـ Terminal SDK بالإضافة إلى Google/Huawei location dependencies.

## 4) AndroidManifest / Permissions المطلوبة

تذكر الوثائق إضافة الصلاحيات التالية:
- `ACCESS_FINE_LOCATION`
- `ACCESS_NETWORK_STATE`
- `INTERNET`
- `READ_PHONE_STATE`
- `NFC`

كما تذكر إضافة `tools:replace="android:allowBackup"` داخل `<application>` وأن يكون `xmlns:tools` موجوداً في `<manifest>`.

## 5) Keystore + PEM Certificate

وثائق NearPay تشرح:
- إنشاء release keystore (أو استخدام الموجود)
- ثم تصدير شهادة عامة بصيغة PEM وإرسالها لـ NearPay

---

## المصادر الرسمية

- Quick Start: https://docs.nearpay.io/sa/en/guides/quick-start
- Generating a Keystore & PEM Certificate for Android: https://docs.nearpay.io/sa/en/guides/generating-keystore-pem-certificate

