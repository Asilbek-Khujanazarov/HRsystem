
using AutoMapper;
using HRsystem.Data;
using HRsystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace AtirAPI.Controllers
{
    // [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public ProductsController(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        /// <summary>
        /// Mahsulotlar ro'yxatini olish
        /// </summary>
        /// <returns>Mahsulotlar ro'yxati</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductDTO>>> GetProducts()
        {
            var products = await _context.Products.ToListAsync();
            return Ok(_mapper.Map<IEnumerable<ProductDTO>>(products));
        }

        /// <summary>
        /// Berilgan ID bo'yicha mahsulotni olish
        /// </summary>
        /// <param name="id">Mahsulot ID si</param>
        /// <returns>Mahsulot ma'lumotlari</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDTO>> GetProduct(int id)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return _mapper.Map<ProductDTO>(product);
        }

        /// <summary>
        /// Yangi mahsulot qo'shish (omborga qo'shiladi, stok yangilanadi)
        /// </summary>
        /// <param name="productCreateDTO">Yangi mahsulot ma'lumotlari</param>
        /// <returns>Yaratilgan mahsulotning ma'lumotlari</returns>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<Product>> PostProduct(ProductCreateDTO productCreateDTO)
        {
            try
            {
                var product = _mapper.Map<Product>(productCreateDTO);
                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
            }
            catch (Exception)
            {
                throw; // Xatolikni middleware qayta ishlaydi
            }
        }

        /// <summary>
        /// Mahsulotni ombordan olish (stokdan kamaytirish)
        /// </summary>
        /// <param name="id">Mahsulot ID si</param>
        /// <param name="quantity">Olmoqchi bo'lgan miqdor</param>
        /// <returns>Olingan mahsulot ma'lumotlari</returns>
        [Authorize(Roles = "Admin")]
        [HttpPost("{id}/take")]
        public async Task<IActionResult> TakeProduct(int id, [FromQuery] int quantity)
        {
            if (quantity <= 0)
            {
                return BadRequest("Miqdor 0 dan katta bo'lishi kerak.");
            }

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
            {
                return NotFound("Mahsulot topilmadi.");
            }

            // Stok miqdorini tekshirish
            if (product.Stock < quantity)
            {
                return BadRequest($"Omborda yetarli mahsulot yo'q. Joriy stok: {product.Stock}, so'ralgan miqdor: {quantity}");
            }

            // Stokni yangilash (kamaytirish)
            product.Stock -= quantity;
            _context.Products.Update(product);
            await _context.SaveChangesAsync();

            return Ok(_mapper.Map<ProductDTO>(product));
        }

         [Authorize(Roles = "Admin")]
        [HttpPost("{id}/Add")]
        public async Task<IActionResult> AddProduct(int id, [FromQuery] int quantity)
        {
         

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
            {
                return NotFound("Mahsulot topilmadi.");
            }

            // Stokni yangilash (kamaytirish)
            product.Stock += quantity;
            _context.Products.Update(product);
            await _context.SaveChangesAsync();

            return Ok(_mapper.Map<ProductDTO>(product));
        }


        /// <summary>
        /// Mahsulotga rasm yuklash
        /// </summary>
        /// <param name="id">Mahsulot ID si</param>
        /// <param name="file">Yuklanayotgan rasm fayli</param>
        /// <param name="hostEnvironment">Host muhitiga oid ma'lumotlar</param>
        /// <returns>Yuklangan rasm bilan yangilangan mahsulot</returns>
        [Authorize(Roles = "Admin")]
        [HttpPost("{id}/upload-image")]
        public async Task<IActionResult> UploadImage(
            int id,
            IFormFile file,
            [FromServices] IWebHostEnvironment hostEnvironment
        )
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound("Mahsulot topilmadi.");
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest("Noto‘g‘ri fayl.");
            }

            var imagePath = Path.Combine(hostEnvironment.WebRootPath, "images");
            if (!Directory.Exists(imagePath))
            {
                Directory.CreateDirectory(imagePath);
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest(
                    "Noto‘g‘ri fayl turi. Faqat JPG, JPEG, PNG va GIF fayllar qabul qilinadi."
                );
            }

            const long maxFileSize = 5 * 1024 * 1024; // 5MB
            if (file.Length > maxFileSize)
            {
                return BadRequest("Fayl hajmi 5MB dan oshib ketdi.");
            }

            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(imagePath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            if (!string.IsNullOrEmpty(product.ImageUrl))
            {
                var oldImagePath = Path.Combine(
                    hostEnvironment.WebRootPath,
                    product.ImageUrl.TrimStart('/')
                );
                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }
            }

            product.ImageUrl = $"/images/{fileName}";
            _context.Products.Update(product);
            await _context.SaveChangesAsync();

            return Ok(_mapper.Map<ProductDTO>(product));
        }

        /// <summary>
        /// Mahsulot ma'lumotlarini yangilash
        /// </summary>
        /// <param name="id">Mahsulot ID si</param>
        /// <param name="productUpdateDTO">Yangilangan mahsulot ma'lumotlari</param>
        /// <returns>Mahsulot yangilanganidan keyingi javob</returns>
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProduct(int id, ProductUpdateDTO productUpdateDTO)
        {
            var existingProduct = await _context.Products.FindAsync(id);
            if (existingProduct == null)
            {
                return NotFound();
            }

            // DTO dagi ma'lumotlarni mavjud productga map qilish
            _mapper.Map(productUpdateDTO, existingProduct);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        /// <summary>
        /// Mahsulotni o'chirish
        /// </summary>
        /// <param name="id">Mahsulot ID si</param>
        /// <returns>O'chirish amali bajarilganidan keyin javob</returns>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }

        /// <summary>
        /// Mahsulotlarni turli kriteriyalarga ko'ra qidirish
        /// </summary>
        /// <param name="name">Mahsulot nomi</param>
        /// <param name="minPrice">Minimal narx</param>
        /// <param name="maxPrice">Maksimal narx</param>
        /// <param name="page">Sahifa raqami</param>
        /// <param name="pageSize">Sahifa hajmi</param>
        /// <param name="sortBy">Saralash mezoni</param>
        /// <param name="ascending">Saralash tartibi (true - o'sish, false - kamayish)</param>
        /// <returns>Qidiruv natijalari va sahifa</returns>
        [HttpGet("search")]
        public async Task<ActionResult<PaginatedList<ProductDTO>>> SearchProducts(
            [FromQuery] string? name,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool ascending = true
        )
        {
            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrEmpty(name))
            {
                query = query.Where(p => p.Name.Contains(name));
            }

            if (minPrice.HasValue)
            {
                query = query.Where(p => p.Price >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= maxPrice.Value);
            }

            if (!string.IsNullOrEmpty(sortBy))
            {
                var validSortFields = new[] { "Name", "Price", "Stock" }; // Valid sort fields
                if (!validSortFields.Contains(sortBy))
                {
                    return BadRequest("Noto'g'ri saralash maydoni. Faqat 'Name', 'Price' yoki 'Stock' ishlatilishi mumkin.");
                }

                query = ascending
                    ? query.OrderBy(p => EF.Property<object>(p, sortBy))
                    : query.OrderByDescending(p => EF.Property<object>(p, sortBy));
            }

            var totalItems = await query.CountAsync();
            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var productDTOs = _mapper.Map<List<ProductDTO>>(products);

            return Ok(
                new PaginatedList<ProductDTO>
                {
                    TotalItems = totalItems,
                    Page = page,
                    PageSize = pageSize,
                    Items = productDTOs
                }
            );
        }

    }
}