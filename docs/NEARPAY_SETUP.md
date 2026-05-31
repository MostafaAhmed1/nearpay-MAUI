# إعداد NearPay وتشغيل الديمو (MAUI)

هذا الملف يشرح **بالخطوات** ما تحتاجه لتشغيل تطبيق الديمو، وما هي **المفاتيح/الملفات/التوكنز** المطلوبة، ومن أين تحصل عليها، وأين تضعها.

## المصادر الرسمية (NearPay)
- Introduction: https://docs.nearpay.io/sa/en/guides/introduction
- Payment Plugin SDK: https://docs.nearpay.io/sa/en/guides/sdk
- Preparing your App: https://docs.nearpay.io/sa/en/guides/preparing-your-app
- Keystore/PEM: https://docs.nearpay.io/sa/en/guides/generating-keystore-pem-certificate

---

## 1) ما الذي يستخدمه هذا المشروع؟

هذا المشروع يستخدم **NearPay Payment Plugin SDK** (الموضح في `guides/sdk`) عن طريق Android Binding.

ملاحظات مهمة:
- عند أول استدعاء لوظائف NearPay قد يتم طلب تثبيت **Payment Plugin** (حسب وثائق NearPay).
- يجب أن يكون الجهاز **Android حقيقي** ويدعم NFC، وليس Emulator، وليس Root.

---

## 2) هل يوجد مفاتيح تجريبية جاهزة؟

عملياً NearPay يوفر لك **Sandbox account** وصلاحيات Users + Terminals من خلال الـ Dashboard/فريق التكامل، وليس “API keys” عامة جاهزة.

بالتالي “التجربة” تكون عبر:
- Sandbox environment داخل SDK.
- مستخدم/Terminal على حساب الـ Sandbox.

---

## 3) الأشياء المطلوبة من NearPay / Dashboard (Sandbox)

### (A) تسجيل تطبيق Android (Package Name)
بدون تسجيل الـ package name لن تستطيع التواصل مع NearPay.

الخطوات:
1. افتح NearPay Sandbox Dashboard: https://sandbox.nearpay.io/#/
2. اذهب إلى **Apps**
3. **Add new App**
4. ضع:
   - **Package Name** (مثل: `com.companyname.nearpayposmauidemo.app`)
   - اسم للتطبيق

> أحياناً NearPay قد يطلب إرسال package name عبر البريد `integration@nearpay.io` حسب مسار التفعيل.

### (B) إعداد Terminal + صلاحيات المستخدم
1. من Dashboard ادخل على **Terminals**
2. Create new terminal
3. أنشئ TRSM Code (6 أرقام hex) حسب متطلبات NearPay
4. اربط الـ terminal بmerchant (موجود أو جديد)
5. افتح تفاصيل terminal ثم **Invite user** (حتى يستطيع يسجل دخول من داخل التطبيق)

---

## 4) Keystore + PEM Certificate (ملف/شهادة ترسلها لـ NearPay)

NearPay يحتاج **PEM Certificate** لتوثيق تطبيقك.

اتبع الدليل الرسمي:
https://docs.nearpay.io/sa/en/guides/generating-keystore-pem-certificate

### ما هي الملفات؟
- **my-release.keystore / .jks**: ملف توقيع Android Release (هذا عندك أنت ولا ترسله).
- **developer_cert.pem**: شهادة public يتم إرسالها لـ NearPay.

### أين أضعها في المشروع؟
**لا تضع keystore داخل الريبو.**  
قم بتخزينه في مكان آمن (password manager + backup).

---

## 5) طرق تسجيل الدخول (Authentication) في Payment Plugin SDK

يدعم عدة طرق (حسب الوثائق):
1. **UserEnter**
2. **Mobile** (مثل `+966...`)
3. **Email**
4. **JWT**

### أين أضع بيانات الدخول في تطبيق الديمو؟
في شاشة الديمو:
- اختر `Auth Mode`
- اكتب القيمة في `Auth Value`
- اضغط `Initialize` ثم `Setup`

---

## 6) JWT: ما الذي أحتاجه وكيف أطلّعه؟

JWT عادة يتم توليده من **backend** الخاص بك ثم تمريره للتطبيق.

المرجع الرسمي:
https://docs.nearpay.io/sa/en/guides/preparing-your-app#allowing-your-user-to-login-using-jwt

ستحتاج من Dashboard:
1. **Client UUID** أو **Merchant UUID** (حسب نوع الـ Dashboard)
2. ملف **pos_key.pem** (Private key للتوقيع)
   - من صفحة **Credentials** اضغط Generate لتحميله
   - زر Generate يظهر مرة واحدة؛ إذا ضاع اطلب Reset من NearPay
3. قيمة **terminal_id / TID** من صفحة **Terminals**

### أين أضع JWT؟
- في الديمو: `Auth Mode = Jwt` و `Auth Value = <JWT>`
- في المشروع الرسمي: استدعِ backend ثم خزّنه مؤقتاً في SecureStorage.

---

## 7) تشغيل عمليات الدفع داخل الديمو

### مبلغ العملية (Amount)
NearPay يطلب المبلغ بصيغة “minor units”:
- `1455` = 14.55

### الأزرار
بعد `Initialize` ثم `Setup`:
- **Purchase**
- **Refund** (يتطلب `Transaction UUID`)
- **Reverse** (يتطلب `Transaction UUID` وغالباً خلال دقيقة حسب الوثائق)
- **Reconcile** (قد يتطلب Admin PIN حسب إعداداتك)

---

## 8) Tokens إضافية (متى أحتاجها؟)

### Payment Plugin SDK (المستخدم حالياً)
- لا يحتاج GitLab private token.
- الاعتماد الأساسي: Dashboard + User/Terminal + Authentication.

### TerminalSDK (مسار مختلف)
لو قررتوا لاحقاً استخدام `TerminalSDK` ستحتاج غالباً:
- **Private token** لمستودع Maven الخاص بـ NearPay على GitLab.
- إعداد Google Play Integrity (Project Number).
- Huawei Safety Detect API Key (اختياري).

---

## 9) أين تجد الكود؟

- واجهة الخدمة: `src/NearpayPosMauiDemo.Core/Abstractions/INearpayService.cs`
- تنفيذ NearPay على Android: `src/NearpayPosMauiDemo.App/Platforms/Android/Services/NearpayServiceAndroid.cs`
- شاشة الديمو: `src/NearpayPosMauiDemo.App/MainPage.xaml`
