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

// تحكم إدارة المنتجات في لوحة التحكم
[Area("Admin")] // تحديد أن هذا التحكم يقع داخل منطقة الأدمن
[Authorize(Roles = "Admin")] // لا يمكن الوصول لهذه الصفحة إلا بحساب أدمن
public class ProductsController : Controller
{
    // نسخة من قاعدة البيانات نستخدمها لقراءة البيانات
    private readonly ApplicationDbContext _context;

    // المنشئ الذي يستقبل قاعدة البيانات من نظام حقن التبعيات
    public ProductsController(ApplicationDbContext context)
    {
        // حفظ نسخة قاعدة البيانات للاستخدام داخل التحكم
        _context = context;
    }

    // دالة عرض قائمة جميع المنتجات في لوحة التحكم
    public async Task<IActionResult> Index(string? searchTerm)
    {
        // نبدأ الاستعلام على جدول المنتجات مع تضمين الفئة
        var query = _context.Products.Include(p => p.Category).AsQueryable();

        // إذا أدخل الأدمن كلمة بحث نقوم بالبحث في اسم المنتج
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            // نحول كلمة البحث إلى صيغة موحدة للمقارنة
            var term = searchTerm.Trim().ToLower();
            // نبحث عن المنتجات التي تحتوي اسمها على كلمة البحث
            query = query.Where(p => p.Name.ToLower().Contains(term));
        }

        // نجلب المنتجات مرتبة حسب الأحدث إضافة
        var products = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();

        // نعيد عرض الصفحة مع قائمة المنتجات وكلمة البحث للاحتفاظ بها في الصفحة
        ViewBag.SearchTerm = searchTerm;
        return View(products);
    }

    // دالة عرض نموذج إضافة منتج جديد
    public async Task<IActionResult> Create()
    {
        // نضع قائمة الفئات المتاحة في القائمة المنسدلة
        ViewBag.Categories = await _context.Categories.ToListAsync();
        // نعيد عرض نموذج الإضافة
        return View();
    }

    // دالة تنفيذ إضافة منتج جديد إلى قاعدة البيانات
    [HttpPost] // الدالة تتعامل مع طلبات POST فقط (إرسال بيانات)
    [ValidateAntiForgeryToken] // حماية من هجمات التزوير
    public async Task<IActionResult> Create(Product product)
    {
        // نتحقق من صحة البيانات المدخلة
        if (ModelState.IsValid)
        {
            // نحدد تاريخ إضافة المنتج
            product.CreatedAt = DateTime.Now;
            // نضيف المنتج إلى قاعدة البيانات
            _context.Add(product);
            // نحفظ التغييرات في قاعدة البيانات
            await _context.SaveChangesAsync();
            // رسالة نجاح تظهر في لوحة التحكم
            TempData["Success"] = "تمت إضافة المنتج بنجاح";
            // نعيد التوجيه إلى قائمة المنتجات
            return RedirectToAction(nameof(Index));
        }

        // إذا كانت البيانات غير صحيحة نعيد عرض النموذج مع الفئات
        ViewBag.Categories = await _context.Categories.ToListAsync();
        return View(product);
    }

    // دالة عرض نموذج تعديل منتج
    public async Task<IActionResult> Edit(int? id)
    {
        // نتحقق من وجود رقم المنتج
        if (id == null)
        {
            // إذا كان الرقم غير موجود نعيد صفحة عدم الوجود
            return NotFound();
        }

        // نبحث عن المنتج في قاعدة البيانات
        var product = await _context.Products.FindAsync(id);

        // إذا لم يوجد المنتج نعيد صفحة عدم الوجود
        if (product == null)
        {
            // صفحة عدم الوجود
            return NotFound();
        }

        // نضع قائمة الفئات المتاحة في القائمة المنسدلة
        ViewBag.Categories = await _context.Categories.ToListAsync();
        // نعيد عرض نموذج التعديل مع بيانات المنتج
        return View(product);
    }

    // دالة تنفيذ تعديل المنتج وحفظ التغييرات
    [HttpPost] // الدالة تتعامل مع طلبات POST فقط (إرسال بيانات)
    [ValidateAntiForgeryToken] // حماية من هجمات التزوير
    public async Task<IActionResult> Edit(int id, Product product)
    {
        // نتأكد من تطابق رقم المنتج مع المعرف
        if (id != product.Id)
        {
            // إذا لم يتطابق نعيد صفحة عدم الوجود
            return NotFound();
        }

        // نتحقق من صحة البيانات المدخلة
        if (ModelState.IsValid)
        {
            // نحفظ تاريخ الإضافة القديم حتى لا يتغير عند التعديل
            var original = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            // إذا وجد المنتج الأصلي نحتفظ بتاريخ إضافته
            if (original != null)
            {
                // إرجاع تاريخ الإضافة الأصلي
                product.CreatedAt = original.CreatedAt;
            }

            // نحدّث بيانات المنتج في قاعدة البيانات
            _context.Update(product);
            // نحفظ التغييرات
            await _context.SaveChangesAsync();
            // رسالة نجاح تظهر في لوحة التحكم
            TempData["Success"] = "تم تعديل المنتج بنجاح";
            // نعيد التوجيه إلى قائمة المنتجات
            return RedirectToAction(nameof(Index));
        }

        // إذا كانت البيانات غير صحيحة نعيد عرض النموذج مع الفئات
        ViewBag.Categories = await _context.Categories.ToListAsync();
        return View(product);
    }

    // دالة عرض صفحة تأكيد حذف منتج
    public async Task<IActionResult> Delete(int? id)
    {
        // نتحقق من وجود رقم المنتج
        if (id == null)
        {
            // إذا كان الرقم غير موجود نعيد صفحة عدم الوجود
            return NotFound();
        }

        // نبحث عن المنتج مع تضمين الفئة
        var product = await _context.Products
            .Include(p => p.Category) // نضم معلومات الفئة
            .FirstOrDefaultAsync(p => p.Id == id);

        // إذا لم يوجد المنتج نعيد صفحة عدم الوجود
        if (product == null)
        {
            // صفحة عدم الوجود
            return NotFound();
        }

        // نعيد عرض صفحة تأكيد الحذف مع بيانات المنتج
        return View(product);
    }

    // دالة تنفيذ حذف المنتج من قاعدة البيانات
    [HttpPost] // الدالة تتعامل مع طلبات POST فقط (إرسال بيانات)
    [ValidateAntiForgeryToken] // حماية من هجمات التزوير
    public async Task<IActionResult> Delete(int id)
    {
        // نبحث عن المنتج في قاعدة البيانات
        var product = await _context.Products.FindAsync(id);

        // إذا كان المنتج موجوداً نقوم بحذفه
        if (product != null)
        {
            // حذف المنتج من قاعدة البيانات
            _context.Products.Remove(product);
            // حفظ التغييرات
            await _context.SaveChangesAsync();
            // رسالة نجاح تظهر في لوحة التحكم
            TempData["Success"] = "تم حذف المنتج بنجاح";
        }

        // نعيد التوجيه إلى قائمة المنتجات
        return RedirectToAction(nameof(Index));
    }
}
