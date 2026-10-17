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
// استيراد نماذج العرض الخاصة بالمشروع
using StoreWebsite.ViewModels;

// مساحة الأسماء الخاصة بتحكمات الأدمن داخل منطقة (Area) الأدمن
namespace StoreWebsite.Areas.Admin.Controllers;

// تحكم لوحة التحكم الرئيسية للأدمن
[Area("Admin")] // تحديد أن هذا التحكم يقع داخل منطقة الأدمن
[Authorize(Roles = "Admin")] // لا يمكن الوصول لهذه الصفحة إلا بحساب أدمن
public class AdminController : Controller
{
    // نسخة من قاعدة البيانات نستخدمها لقراءة البيانات
    private readonly ApplicationDbContext _context;

    // المنشئ الذي يستقبل قاعدة البيانات من نظام حقن التبعيات
    public AdminController(ApplicationDbContext context)
    {
        // حفظ نسخة قاعدة البيانات للاستخدام داخل التحكم
        _context = context;
    }

    // دالة عرض لوحة التحكم مع الإحصائيات
    public async Task<IActionResult> Index()
    {
        // ننشئ نموذج عرض جديد للوحة التحكم
        var model = new DashboardViewModel();

        // نحسب عدد المنتجات الكلي في المتجر
        model.TotalProducts = await _context.Products.CountAsync();

        // نحسب عدد الفئات الكلي في المتجر
        model.TotalCategories = await _context.Categories.CountAsync();

        // نحسب عدد الطلبات الكلي
        model.TotalOrders = await _context.Orders.CountAsync();

        // نحسب إجمالي إيرادات المتجر من الطلبات الناجحة
        // (نجمع قيمة الطلبات ما عدا الملغاة)
        model.TotalRevenue = await _context.Orders
            .Where(o => o.Status != OrderStatus.Cancelled) // استثناء الطلبات الملغاة
            .SumAsync(o => o.TotalAmount);

        // نحسب عدد المستخدمين الكلي
        model.TotalUsers = await _context.Users.CountAsync();

        // نجلب أحدث 5 طلبات لعرضها في لوحة التحكم
        model.RecentOrders = await _context.Orders
            .OrderByDescending(o => o.OrderDate) // نرتب من الأحدث
            .Take(5) // نأخذ أول 5 طلبات
            .ToListAsync();

        // نجلب أكثر 5 منتجات مبيعاً
        model.BestSellingProducts = await _context.Products
            .OrderByDescending(p => p.NumberOfSales) // نرتب حسب المبيعات
            .Take(5) // نأخذ أول 5 منتجات
            .ToListAsync();

        // نعيد عرض لوحة التحكم مع النموذج
        return View(model);
    }
}
