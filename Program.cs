// استيراد حزمة Identity للعمل مع المستخدمين والأدوار
using Microsoft.AspNetCore.Identity;
// استيراد حزمة Entity Framework للعمل مع قاعدة البيانات
using Microsoft.EntityFrameworkCore;
// استيراد قاعدة البيانات الخاصة بالمشروع
using StoreWebsite.Data;
// استيراد نماذج البيانات الخاصة بالمشروع (نحتاج كلاس المستخدم لربطه بنظام Identity)
using StoreWebsite.Models;
// استيراد خدمة السلة الخاصة بالمشروع
using StoreWebsite.Services;

// نقوم ببناء التطبيق ونقرأ الإعدادات من ملف appsettings.json
var builder = WebApplication.CreateBuilder(args);

// إضافة خدمات MVC (Models - Views - Controllers) للنظام
builder.Services.AddControllersWithViews();

// إضافة قاعدة البيانات (SQL Server LocalDB) إلى نظام حقن التبعيات
// نقرأ سلسلة الاتصال بقاعدة البيانات من ملف الإعدادات
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// إضافة نظام Identity للمستخدمين والأدوار
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // إعدادات كلمة المرور
    options.Password.RequiredLength = 6; // الحد الأدنى لطول كلمة المرور هو 6 أحرف
    options.Password.RequireDigit = false; // لا نطالب برقم إلزامي في كلمة المرور
    options.Password.RequireUppercase = false; // لا نطالب بحرف كبير إلزامي
    options.Password.RequireNonAlphanumeric = false; // لا نطالب برموز خاصة إلزامية
})
    // نربط نظام Identity بقاعدة البيانات الخاصة بالمشروع
    .AddEntityFrameworkStores<ApplicationDbContext>()
    // إضافة التوكنات الخاصة بتأكيد البريد الإلكتروني
    .AddDefaultTokenProviders();

// إضافة خدمة الوصول إلى الـ HttpContext من أي مكان في التطبيق
// (نحتاجها في خدمة السلة للوصول إلى الـ Session)
builder.Services.AddHttpContextAccessor();

// إضافة خدمة الـ Session لتخزين سلة التسوق وبيانات المستخدم المؤقتة
builder.Services.AddSession(options =>
{
    // مدة بقاء الجلسة بدون نشاط قبل حذفها (30 دقيقة)
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    // تعيين سلة التسوق كمتغير متاح في جميع الصفحات (لعدد المنتجات في الشريط)
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// تسجيل خدمة السلة في نظام حقن التبعيات
// AddScoped يعني إنشاء نسخة واحدة لكل طلب HTTP
builder.Services.AddScoped<CartService>();

// نقوم ببناء التطبيق بعد تسجيل جميع الخدمات
var app = builder.Build();

// ------------------- خط أنابيب معالجة الطلبات -------------------

// إذا لم يكن التطبيق في بيئة التطوير
if (!app.Environment.IsDevelopment())
{
    // استخدام صفحة خطأ عامة عند حدوث أي استثناء
    app.UseExceptionHandler("/Home/Error");
    // تفعيل HSTS لأمان الاتصال في بيئة الإنتاج
    app.UseHsts();
}

// تحويل الطلبات إلى بروتوكول HTTPS الآمن
app.UseHttpsRedirection();
// تمكين معالجة الملفات الثابتة (صور، CSS، JavaScript)
app.UseStaticFiles();
// تمكين بدء التوجيه بين الصفحات
app.UseRouting();

// تفعيل الـ Session في معالجة الطلبات
app.UseSession();

// تفعيل التعرف على المستخدمين (من يفعل ماذا)
app.UseAuthentication();
// تفعيل التحقق من صلاحيات الوصول للمستخدمين
app.UseAuthorization();

// توجيه الطلبات إلى منطقة الأدمن أولاً (مثل /Admin/Products)
app.MapControllerRoute(
    // اسم التوجيه الخاص بالمناطق (Areas)
    name: "areas",
    // نمط المسار: اسم المنطقة إذا كانت موجودة / اسم التحكم / اسم الدالة
    // التحكم الافتراضي داخل الأدمن هو Admin (حتى تفتح لوحة التحكم عند زيارة /Admin)
    pattern: "{area:exists}/{controller=Admin}/{action=Index}/{id?}");

// توجيه الطلبات إلى الصفحات حسب المسار (المسار الافتراضي العادي)
app.MapControllerRoute(
    // اسم التوجيه الافتراضي
    name: "default",
    // نمط المسار: اسم التحكم / اسم الدالة / رقم اختياري
    pattern: "{controller=Home}/{action=Index}/{id?}");

// إنشاء نطاق (Scope) للوصول إلى الخدمات المسجلة في نظام حقن التبعيات
using (var scope = app.Services.CreateScope())
{
    // استدعاء دالة إدخال البيانات الأولية (إنشاء قاعدة البيانات وتعبئتها)
    // تمرر الخدمات اللازمة لتهيئة قاعدة البيانات
    await DbInitializer.InitializeAsync(scope.ServiceProvider);
}

// تشغيل التطبيق والاستماع للطلبات القادمة
app.Run();
