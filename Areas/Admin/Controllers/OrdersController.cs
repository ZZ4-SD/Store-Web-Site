// استيراد مكتبة Entity Framework للتعامل مع قاعدة البيانات
using Microsoft.EntityFrameworkCore;
// استيراد MVC الأساسي
using Microsoft.AspNetCore.Mvc;
// استيراد الحزم الخاصة بالتحكم في المستخدم الحالي
using Microsoft.AspNetCore.Authorization;
// استيراد قاعدة البيانات الخاصة بالمشروع
using StoreWebsite.Data;
// استيراد نماذج البيانات الخاصة بالمشروع
using StoreWebsite.Models;

// مساحة الأسماء الخاصة بتحكمات الأدمن داخل منطقة (Area) الأدمن
namespace StoreWebsite.Areas.Admin.Controllers;

// تحكم إدارة الطلبات في لوحة التحكم
[Area("Admin")] // تحديد أن هذا التحكم يقع داخل منطقة الأدمن
[Authorize(Roles = "Admin")] // لا يمكن الوصول لهذه الصفحة إلا بحساب أدمن
public class OrdersController : Controller
{
    // نسخة من قاعدة البيانات نستخدمها لقراءة البيانات
    private readonly ApplicationDbContext _context;

    // المنشئ الذي يستقبل قاعدة البيانات من نظام حقن التبعيات
    public OrdersController(ApplicationDbContext context)
    {
        // حفظ نسخة قاعدة البيانات للاستخدام داخل التحكم
        _context = context;
    }

    // دالة عرض قائمة جميع الطلبات
    public async Task<IActionResult> Index(string? status)
    {
        // نبدأ الاستعلام على جدول الطلبات مع تضمين عناصرها والمستخدم
        var query = _context.Orders
            .Include(o => o.OrderItems) // نضم عناصر كل طلب
            .Include(o => o.User)       // نضم بيانات المستخدم صاحب الطلب
            .AsQueryable();

        // إذا تم اختيار فلترة بحالة معينة
        if (!string.IsNullOrEmpty(status) && status != "All")
        {
            // نرجع فقط الطلبات التي حالتها مطابقة للمختارة
            query = query.Where(o => o.Status == status);
        }

        // نجلب الطلبات مرتبة من الأحدث للأقدم
        var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();

        // نضع الحالة المختارة في المتغير ViewBag لتظهر في الصفحة
        ViewBag.Status = status ?? "All";
        // نعيد عرض الصفحة مع قائمة الطلبات
        return View(orders);
    }

    // دالة عرض تفاصيل طلب محدد
    public async Task<IActionResult> Details(int? id)
    {
        // نتحقق من وجود رقم الطلب
        if (id == null)
        {
            // إذا كان الرقم غير موجود نعيد صفحة عدم الوجود
            return NotFound();
        }

        // نبحث عن الطلب مع تضمين عناصره والمستخدم
        var order = await _context.Orders
            .Include(o => o.OrderItems) // نضم عناصر الطلب
            .Include(o => o.User)       // نضم بيانات المستخدم
            .FirstOrDefaultAsync(o => o.Id == id);

        // إذا لم يوجد الطلب نعيد صفحة عدم الوجود
        if (order == null)
        {
            // صفحة عدم الوجود
            return NotFound();
        }

        // نعيد عرض صفحة التفاصيل مع بيانات الطلب
        return View(order);
    }

    // دالة تحديث حالة الطلب
    [HttpPost] // الدالة تتعامل مع طلبات POST فقط (إرسال بيانات)
    [ValidateAntiForgeryToken] // حماية من هجمات التزوير
    public async Task<IActionResult> UpdateStatus(int id, string status)
    {
        // نبحث عن الطلب في قاعدة البيانات
        var order = await _context.Orders.FindAsync(id);

        // إذا كان الطلب موجوداً نقوم بتحديث حالته
        if (order != null)
        {
            // تعيين الحالة الجديدة للطلب
            order.Status = status;
            // حفظ التغييرات في قاعدة البيانات
            await _context.SaveChangesAsync();
            // رسالة نجاح تظهر في لوحة التحكم
            TempData["Success"] = "تم تحديث حالة الطلب بنجاح";
        }

        // نعيد التوجيه إلى صفحة تفاصيل الطلب
        return RedirectToAction(nameof(Details), new { id });
    }
}
