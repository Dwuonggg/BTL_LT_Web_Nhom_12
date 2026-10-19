using BTL_LT_Web_Nhom_12.Models.Account;
using BTL_LT_Web_Nhom_12.Models.Menu;
using BTL_LT_Web_Nhom_12.Models.Order;
using Microsoft.EntityFrameworkCore;
using BTL_LT_Web_Nhom_12.Models.Voucher;
using BTL_LT_Web_Nhom_12.Models.Poster;
using BTL_LT_Web_Nhom_12.Models.Setting;
using PaymentModel = BTL_LT_Web_Nhom_12.Models.Payment.Payment;
namespace BTL_LT_Web_Nhom_12.Database
{
    public class WebDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public WebDbContext(DbContextOptions<WebDbContext> options) : base(options){}
        // Account
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Customer> Customers { get; set; }
        
        // Menu
        public DbSet<Category> Categories { get; set; }
        public DbSet<Food> Foods { get; set; }
        public DbSet<Combo> Combos { get; set; }
        public DbSet<ComboDetail> ComboDetails { get; set; }

        // Order
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }

        // Paymeent
        public DbSet<PaymentModel> Payments { get; set; }

        // Voucher
        public DbSet<Voucher> Vouchers { get; set; }
        public DbSet<VoucherUsage> VoucherUsages { get; set; }

        // Banner
        public DbSet<Banner> Banners { get; set; }

        // OpeningHour
        public DbSet<OpeningHour> OpeningHours { get; set; }
        public DbSet<PasswordResetOtp> PasswordResetOtps { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Tên đăng nhập Admin không trùng
            modelBuilder.Entity<Admin>()
                .HasIndex(a => a.Username)
                .IsUnique();

            // Số điện thoại khách hàng không trùng
            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.Phone)
                .IsUnique();

            // Tên danh mục không trùng
            modelBuilder.Entity<Category>()
                .HasIndex(c => c.Name)
                .IsUnique();

            // Mã voucher không trùng
            modelBuilder.Entity<Voucher>()
                .HasIndex(v => v.Code)
                .IsUnique();

            // Mỗi thứ trong tuần chỉ có một cấu hình
            modelBuilder.Entity<OpeningHour>()
                .HasIndex(o => o.DayOfWeek)
                .IsUnique();
            // ComboId và FoodID không trùng
            modelBuilder.Entity<ComboDetail>()
                .HasIndex(cd => new { cd.ComboId, cd.FoodId })
                .IsUnique();

            // Mỗi dòng OrderDetail đại diện cho một loại sản phẩm
            modelBuilder.Entity<OrderDetail>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_OrderDetail_FoodOrCombo",
                    "([FoodId] IS NOT NULL AND [ComboId] IS NULL) OR " +
                    "([FoodId] IS NULL AND [ComboId] IS NOT NULL)"
                ));

            // Không cho xóa khách hàng khi còn đơn hàng
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.NoAction);

            // Không cho xóa món ăn đang có trong giỏ hàng
            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Food)
                .WithMany()
                .HasForeignKey(od => od.FoodId)
                .OnDelete(DeleteBehavior.NoAction);

            // Không cho xóa combo đang có trong giỏ hàng
            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Combo)
                .WithMany()
                .HasForeignKey(od => od.ComboId)
                .OnDelete(DeleteBehavior.NoAction);

            // Không tự động xóa payment khi xóa order
            modelBuilder.Entity<PaymentModel>()
                .HasOne(p => p.Order)
                .WithOne(o => o.Payment)
                .HasForeignKey<PaymentModel>(p => p.OrderId)
                .OnDelete(DeleteBehavior.NoAction);

            // Mỗi tài khoản chỉ được dùng voucher đúng 1 lần duy nhất
            modelBuilder.Entity<VoucherUsage>()
                .HasIndex(vu => new { vu.VoucherId, vu.CustomerId })
                .IsUnique();

            modelBuilder.Entity<VoucherUsage>()
                .HasOne(vu => vu.Voucher)
                .WithMany(v => v.VoucherUsages)
                .HasForeignKey(vu => vu.VoucherId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<VoucherUsage>()
                .HasOne(vu => vu.Customer)
                .WithMany()
                .HasForeignKey(vu => vu.CustomerId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<VoucherUsage>()
                .HasOne(vu => vu.Order)
                .WithOne()
                .HasForeignKey<VoucherUsage>(vu => vu.OrderId)
                .OnDelete(DeleteBehavior.NoAction);

            // Category - Food
            modelBuilder.Entity<Food>()
                .HasOne(f => f.Category)
                .WithMany(c => c.Foods)
                .HasForeignKey(f => f.CategoryId)
                .OnDelete(DeleteBehavior.NoAction);
  
            // Combo - ComboDetail
            modelBuilder.Entity<ComboDetail>()
                .HasOne(cd => cd.Combo)
                .WithMany(c => c.ComboDetails)
                .HasForeignKey(cd => cd.ComboId)
                .OnDelete(DeleteBehavior.NoAction);

            // Food - ComboDetail
            modelBuilder.Entity<ComboDetail>()
                .HasOne(cd => cd.Food)
                .WithMany(f => f.ComboDetails)
                .HasForeignKey(cd => cd.FoodId)
                .OnDelete(DeleteBehavior.NoAction);

            // Order - OrderDetail
            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Order)
                .WithMany(o => o.OrderDetails)
                .HasForeignKey(od => od.OrderId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<PasswordResetOtp>()
                .HasOne<Customer>()
                .WithMany()
                .HasForeignKey(otp => otp.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
