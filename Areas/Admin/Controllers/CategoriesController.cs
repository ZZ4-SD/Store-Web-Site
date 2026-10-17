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

// تحكم إدارة الفئات (الأقسام) في لوحة التحكم
[Area("Admin")] // تحديد أن هذا التحكم يقع داخل منطقة الأدمن
[Authorize(Roles = "Admin")] // لا يمكن الوصول لهذه الصفحة إلا بحساب أدمن
public class CategoriesController : Controller
{
    // نسخة من قاعدة البيانات نستخدمها لقراءة البيانات
    private readonly ApplicationDbContext _context;

    // المنشئ الذي يستقبل قاعدة البيانات من نظام حقن التبعيات
    public CategoriesController(ApplicationDbContext context)
    {
        // حفظ نسخة قاعدة البيانات للاستخدام داخل التحكم
        _context = context;
    }

    // دالة عرض قائمة الفئات في لوحة التحكم
    public async Task<IActionResult> Index()
    {
        // نجلب جميع الفئات من قاعدة البيانات مع عدد المنتجات في كل فئة
        var categories = await _context.Categories
            .Include(c => c.Products) // نضم منتجات كل فئة لحساب العدد
            .OrderBy(c => c.Name)     // نرتب الفئات أبجدياً
            .ToListAsync();

        // نعيد عرض الصفحة مع قائمة الفئات
        return View(categories);
    }

    // دالة عرض نموذج إضافة فئة جديدة
    public IActionResult Create()
    {
        // نعيد عرض نموذج الإضافة
        return View();
    }

    // دالة تنفيذ إضافة فئة جديدة إلى قاعدة البيانات
    [HttpPost] // الدالة تتعامل مع طلبات POST فقط (إرسال بيانات)
    [ValidateAntiForgeryToken] // حماية من هجمات التزوير
    public async Task<IActionResult> Create(Category category)
    {
        // نتحقق من صحة البيانات المدخلة
        if (ModelState.IsValid)
        {
            // نضيف الفئة الجديدة إلى قاعدة البيانات
            _context.Add(category);
            // نحفظ التغييرات
            await _context.SaveChangesAsync();
            // رسالة نجاح تظهر في لوحة التحكم
            TempData["Success"] = "تمت إضافة الفئة بنجاح";
            // نعيد التوجيه إلى قائمة الفئات
            return RedirectToAction(nameof(Index));
        }

        // إذا كانت البيانات غير صحيحة نعيد عرض النموذج مع الأخطاء
        return View(category);
    }

    // دالة عرض نموذج تعديل فئة
    public async Task<IActionResult> Edit(int? id)
    {
        // نتحقق من وجود رقم الفئة
        if (id == null)
        {
            // إذا كان الرقم غير موجود نعيد صفحة عدم الوجود
            return NotFound();
        }

        // نبحث عن الفئة في قاعدة البيانات
        var category = await _context.Categories.FindAsync(id);

        // إذا لم توجد الفئة نعيد صفحة عدم الوجود
        if (category == null)
        {
            // صفحة عدم الوجود
            return NotFound();
        }

        // نعيد عرض نموذج التعديل مع بيانات الفئة
        return View(category);
    }

    // دالة تنفيذ تعديل الفئة وحفظ التغييرات
    [HttpPost] // الدالة تتعامل مع طلبات POST فقط (إرسال بيانات)
    [ValidateAntiForgeryToken] // حماية من هجمات التزوير
    public async Task<IActionResult> Edit(int id, Category category)
    {
        // نتأكد من تطابق رقم الفئة مع المعرف
        if (id != category.Id)
        {
            // إذا لم يتطابق نعيد صفحة عدم الوجود
            return NotFound();
        }

        // نتحقق من صحة البيانات المدخلة
        if (ModelState.IsValid)
        {
            // نحدّث بيانات الفئة في قاعدة البيانات
            _context.Update(category);
            // نحفظ التغييرات
            await _context.SaveChangesAsync();
            // رسالة نجاح تظهر في لوحة التحكم
            TempData["Success"] = "تم تعديل الفئة بنجاح";
            // نعيد التوجيه إلى قائمة الفئات
            return RedirectToAction(nameof(Index));
        }

        // إذا كانت البيانات غير صحيحة نعيد عرض النموذج مع الأخطاء
        return View(category);
    }

    // دالة تنفيذ حذف فئة من قاعدة البيانات
    [HttpPost] // الدالة تتعامل مع طلبات POST فقط (إرسال بيانات)
    [ValidateAntiForgeryToken] // حماية من هجمات التزوير
    public async Task<IActionResult> Delete(int id)
    {
        // نبحث عن الفئة مع تضمين المنتجات
        var category = await _context.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id);

        // إذا كانت الفئة موجودة نقوم بحذفها
        if (category != null)
        {
            // نتحقق من أن الفئة لا تحتوي على منتجات قبل الحذف
            if (category.Products != null && category.Products.Any())
            {
                // رسالة تحذير لأن الفئة تحتوي منتجات
                TempData["Error"] = "لا يمكن حذف فئة تحتوي على منتجات. انقل المنتجات لفئة أخرى أولاً";
            }
            else
            {
                // حذف الفئة من قاعدة البيانات
                _context.Categories.Remove(category);
                // حفظ التغييرات
                await _context.SaveChangesAsync();
                // رسالة نجاح تظهر في لوحة التحكم
                TempData["Success"] = "تم حذف الفئة بنجاح";
            }
        }

        // نعيد التوجيه إلى قائمة الفئات
        return RedirectToAction(nameof(Index));
    }
}
