# NearpayPosMauiDemo (MAUI + NearPay)

تطبيق تجريبي **شاشة واحدة** مبني بـ **.NET MAUI (net10.0)** لربط وظائف الدفع من NearPay (الـ **Payment Plugin SDK**) على أجهزة **POS / Tap to Pay on Phone**.

> ملاحظة: هذا المشروع يعتمد على **المصادر الرسمية** من NearPay:
> - https://docs.nearpay.io/sa/en/guides/introduction  
> - https://docs.nearpay.io/sa/en/guides/sdk  
> - https://docs.nearpay.io/sa/en/guides/generating-keystore-pem-certificate

## ملف توثيق الإعداد (مهم)

راجع الخطوات التفصيلية هنا:
- `docs/NEARPAY_SETUP.md`

## 1) المتطلبات من NearPay (Sandbox / Production)

حسب وثائق NearPay:

1. إرسال **Android package name** لفريق NearPay (أو تسجيله في Dashboard حسب نوع التكامل).
2. الحصول على وصول للـ **Sandbox** (مستخدمين + terminals).
3. جهاز Android حقيقي يدعم NFC للتجربة.

## 2) توليد Keystore + PEM Certificate

اتبع دليل NearPay الرسمي:
https://docs.nearpay.io/sa/en/guides/generating-keystore-pem-certificate

الـ PEM يتم إرساله لـ NearPay (لا ترسل الـ keystore أو كلمات المرور).

## 3) هيكل المشروع (Clean-ish / قابل للنقل)

داخل `src/`:

- `NearpayPosMauiDemo.Core`  
  واجهات + Models عامة (لا تحتوي كود خاص بالـ Android).

- `NearpayPosMauiDemo.NearpayBindings.Android`  
  Android Binding Library لِـ NearPay SDK (مبنية على Maven artifacts).

- `NearpayPosMauiDemo.App`  
  تطبيق MAUI (شاشة واحدة) + MVVM + DI، مع تنفيذ Android في:
  `Platforms/Android/Services/NearpayServiceAndroid.cs`

## 4) تشغيل التطبيق (Android)

من جذر المشروع:

```powershell
dotnet build .\src\NearpayPosMauiDemo.App\NearpayPosMauiDemo.App.csproj -c Debug -f net10.0-android
dotnet run   .\src\NearpayPosMauiDemo.App\NearpayPosMauiDemo.App.csproj -c Debug -f net10.0-android
```

## 5) طريقة الاستخدام داخل الشاشة

الشاشة تحتوي:

- **تهيئة**: Environment + AuthMode + AuthValue (JWT/Email/Mobile) + Locale  
  ثم أزرار: `Initialize` ثم `Setup`

- **حقول عملية**:
  - Amount (minor) مثال: `1455` = 14.55
  - Customer Ref (اختياري)
  - Transaction UUID (مطلوب لـ Refund/Reverse)
  - Admin PIN (اختياري)
  - Flags: Receipt UI / Reversal / Edit Refund UI / UI Dismiss

- أزرار عمليات:
  - Purchase
  - Refund
  - Reverse
  - Reconcile

النتيجة تظهر في Log داخل نفس الشاشة.

## 6) Android Permissions

تم إضافة الصلاحيات المطلوبة في:
`src/NearpayPosMauiDemo.App/Platforms/Android/AndroidManifest.xml`

وتشمل: Location / Internet / Network State / Read Phone State / NFC

## 7) ملاحظات مهمة

- NearPay عند أول استدعاء سيطلب تثبيت **Payment Plugin** (حسب وثائقهم).
- يجب أن يكون الجهاز غير Root وغير Emulator (NearPay لديه attestation صارم).
- عند نجاح التجربة يمكن نقل:
  - `Core` + `NearpayServiceAndroid` + الـ Binding project إلى مشروعكم الرسمي.
