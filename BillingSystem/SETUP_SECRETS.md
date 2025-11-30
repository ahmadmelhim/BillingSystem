# 🔐 إعداد الأسرار للمشروع (User Secrets Setup)

## ⚠️ مهم جداً
**لا تستخدم المشروع بدون إعداد الأسرار أولاً!**

تم إزالة جميع القيم الحساسة من `appsettings.json` لأسباب أمنية.
يجب عليك إعداد User Secrets قبل تشغيل المشروع.

---

## 📝 خطوات الإعداد

### 1. فتح Terminal في مجلد المشروع
```bash
cd c:\Users\moham\source\repos\BillingSystem\BillingSystem\BillingSystem
```

### 2. تهيئة User Secrets
```bash
dotnet user-secrets init
```

### 3. إضافة مفتاح JWT
استبدل `YOUR_VERY_STRONG_SECRET_KEY_HERE` بمفتاح قوي (على الأقل 32 حرف عشوائي):

```bash
dotnet user-secrets set "Jwt:Key" "YOUR_VERY_STRONG_SECRET_KEY_HERE_AT_LEAST_32_CHARS"
```

**أمثلة لمفاتيح قوية:**
- استخدم مولد كلمات مرور عشوائية
- مثال: `K8f#mN2$pQ9@xL5&vR7!wT3^yU6*zH4%`

### 4. إضافة كلمة مرور Gmail
إذا كنت تستخدم Gmail للبريد الإلكتروني:

1. اذهب إلى: https://myaccount.google.com/apppasswords
2. أنشئ App Password جديد
3. انسخ الكود المكون من 16 رقم
4. أضفه:

```bash
dotnet user-secrets set "Email:Password" "YOUR_16_DIGIT_APP_PASSWORD"
```

---

## ✅ التحقق من الإعداد

لعرض جميع الأسرار المحفوظة:
```bash
dotnet user-secrets list
```

يجب أن ترى:
```
Email:Password = ****************
Jwt:Key = ********************************
```

---

## 🚀 تشغيل المشروع

بعد إعداد الأسرار، يمكنك تشغيل المشروع بشكل طبيعي:
```bash
dotnet run
```

---

## 📦 للإنتاج (Production)

في بيئة الإنتاج، **لا تستخدم User Secrets!**

استخدم أحد الخيارات التالية:
1. **Environment Variables** (الأفضل)
2. **Azure Key Vault** (للـ Azure)
3. **AWS Secrets Manager** (للـ AWS)

### مثال: Environment Variables
```bash
# Windows
set Jwt__Key=YOUR_SECRET_KEY
set Email__Password=YOUR_PASSWORD

# Linux/Mac
export Jwt__Key="YOUR_SECRET_KEY"
export Email__Password="YOUR_PASSWORD"
```

---

## 🔒 ملاحظات أمنية

- ✅ User Secrets تُحفظ خارج المشروع (لا تُرفع على Git)
- ✅ appsettings.json آمن للرفع على Git
- ❌ لا تكتب أبداً أسرار حقيقية في appsettings.json
- ❌ لا تشارك User Secrets مع أحد

---

## 🆘 استكشاف الأخطاء

### خطأ: "Key not found"
**الحل**: تأكد من إعداد `Jwt:Key` باستخدام الأمر أعلاه

### خطأ: "Invalid credentials" عند إرسال Email
**الحل**: تأكد من إعداد `Email:Password` بشكل صحيح

### خطأ: "User secrets not initialized"
**الحل**: شغّل `dotnet user-secrets init` أولاً

---

## 📚 مصادر إضافية
- [Safe storage of app secrets in development](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [Configuration in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
