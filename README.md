# 💼 BillingSystem - نظام الفوترة

## 📋 نظرة عامة

نظام فوترة شامل مبني على **ASP.NET Core 8** و**Blazor Server** 

## ✨ الميزات

- 📊 إدارة الفواتير (إنشاء، تعديل، تصدير PDF، إرسال Email)
- 👥 إدارة العملاء
- 💰 إدارة المدفوعات المتعددة
- 📈 Dashboard تفاعلي مع تقارير
- 🔐 نظام مصادقة JWT مع عزل بيانات
- 🌍 دعم العربية والإنجليزية
- ⚡ تحسينات الأداء (Indexes, AsNoTracking)
- 📝 Logging شامل
- 🔒 Concurrency Control

## 🛠️ التقنيات

- ASP.NET Core 8.0
- Blazor Server
- SQL Server + EF Core
- MudBlazor
- QuestPDF
- JWT Authentication

## 🚀 التشغيل

```bash
# تحديث قاعدة البيانات
cd BillingSystem
dotnet ef database update

# تشغيل المشروع
dotnet run
```

افتح: `https://localhost:7060`

## 🔑 حسابات افتراضية

| Role | Email | Password |
|------|-------|----------|
| Admin | admin@billing.com | Admin@123 |
| Accountant | accountant@billing.com | Acc@123 |

## ⚙️ إعداد البريد الإلكتروني

عدّل `appsettings.json`:

```json
{
  "Email": {
    "FromAddress": "your-email@example.com",
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "EnableSsl": true,
    "Username": "your-email",
    "Password": "your-app-password"
  }
}
```

## 📁 البنية

```
BillingSystem/
├── Core/                    # Models, DTOs, Interfaces
├── Infrastructure/          # Services, Data, Configuration
├── Features/               # Blazor Pages (Vertical Slices)
├── Controllers/            # API Controllers
└── Shared/                 # Layouts & Components
```

## ✅ التحسينات المطبقة

- ✅ **Logging**: شامل في جميع Services
- ✅ **Security**: [Authorize] على API endpoints
- ✅ **Performance**: Database Indexes + AsNoTracking
- ✅ **Data Integrity**: RowVersion للـ Concurrency Control

## 📞 الدعم

للمساعدة، افتح Issue على GitHub

---

**License**: MIT | Developed with ❤️ using ASP.NET Core & Blazor
