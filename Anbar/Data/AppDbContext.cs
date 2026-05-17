using Anbar.Entities;
using Anbar.Entities.Enum;
using Microsoft.EntityFrameworkCore;

namespace Anbar.Data
{
    // Bu class Entity Framework Core-un əsas mərkəzidir.
    // Bütün entity-lər database ilə burada əlaqələndirilir.
    public class AppDbContext : DbContext
    {
        // Constructor - connection string buradan gəlir
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }
        public DbSet<CustomerPayment> CustomerPayments { get; set; }
        // =========================
        // 🔐 LOGIN
        // =========================
        // YENI:
        // Dinamik ölçü vahidləri.
        // Enum deyil, database table kimi saxlanır.
        // YENI:
        // Enterprise tax modeli.
        public DbSet<Tax> Taxes { get; set; }
        public DbSet<ProductTax> ProductTaxes { get; set; }
        public DbSet<InvoiceItemTax> InvoiceItemTaxes { get; set; }
        public DbSet<Unit> Units { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<LocalPurchaseSetting> LocalPurchaseSettings { get; set; }
        public DbSet<LocalPurchaseSettingValue> LocalPurchaseSettingValues { get; set; }
        public DbSet<AdminUser> AdminUsers { get; set; }
        // YENI:
        public DbSet<StockBatch> StockBatches { get; set; }
        // =========================
        // 📦 CORE ANBAR
        // =========================
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<Shelf> Shelves { get; set; }
        public DbSet<ShelfStock> ShelfStocks { get; set; }

        // YENI:
        // Qaimə əlavə xərcləri və xərc detalları.
        public DbSet<ExpenseType> ExpenseTypes { get; set; }
        public DbSet<ExpenseTypeFieldDefinition> ExpenseTypeFieldDefinitions { get; set; }
        public DbSet<InvoiceExpense> InvoiceExpenses { get; set; }
        public DbSet<InvoiceExpenseFieldValue> InvoiceExpenseFieldValues { get; set; }

        // YENI:
        // Təchizatçı ödəniş tarixçəsi.
        public DbSet<SupplierPayment> SupplierPayments { get; set; }

        // =========================
        // 📦 MƏHSULLAR
        // =========================
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }

        // =========================
        // 🧩 ATTRIBUTES
        // =========================
        public DbSet<AttributeDefinition> AttributeDefinitions { get; set; }
        public DbSet<AttributeValue> AttributeValues { get; set; }
        public DbSet<ProductAttribute> ProductAttributes { get; set; }

        // =========================
        // 👥 TƏRƏFLƏR
        // =========================
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Customer> Customers { get; set; }

        // =========================
        // 🧾 QAİMƏ
        // =========================
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceItem> InvoiceItems { get; set; }

        // YENI:
        // İdxal, ödəniş, vergi və maya dəyəri strukturu üçün yeni qaimə entity-ləri.
        public DbSet<InvoiceImportInfo> InvoiceImportInfos { get; set; }
        public DbSet<InvoicePayment> InvoicePayments { get; set; }
        public DbSet<InvoiceTax> InvoiceTaxes { get; set; }
        public DbSet<InvoiceExpenseAllocation> InvoiceExpenseAllocations { get; set; }

        // YENI:
        // Vergi/rüsumların məhsullara paylanması.
        public DbSet<InvoiceTaxAllocation> InvoiceTaxAllocations { get; set; }

        // YENI:
        // Qaimənin yekun maya dəyəri summary-si.
        public DbSet<InvoiceCostSummary> InvoiceCostSummaries { get; set; }

        // YENI:
        // Dynamic idxal/qaimə sahələrinin dəyərləri.
        public DbSet<InvoiceDynamicFieldValue> InvoiceDynamicFieldValues { get; set; }

        // YENI:
        // Təchizatçı/müştəri borc tarixçəsi və valyuta məzənnələri.
        public DbSet<SupplierBalanceTransaction> SupplierBalanceTransactions { get; set; }
        public DbSet<CustomerBalanceTransaction> CustomerBalanceTransactions { get; set; }
        public DbSet<CurrencyRate> CurrencyRates { get; set; }

        // =========================
        // 🧩 SHELF ATTRIBUTES
        // =========================
        public DbSet<ShelfAttributeDefinition> ShelfAttributeDefinitions { get; set; }
        public DbSet<ShelfAttributeValue> ShelfAttributeValues { get; set; }

        // =========================
        // 🔄 STOK HƏRƏKƏTLƏRİ
        // =========================
        public DbSet<StockMovement> StockMovements { get; set; }

        // =========================
        // ⚙️ AYARLAR + LOG
        // =========================
        // YENI:
        // Yeni enterprise ayar entity-ləri.
        public DbSet<WarehouseSetting> WarehouseSettings { get; set; }
        public DbSet<InvoiceSetting> InvoiceSettings { get; set; }
        public DbSet<StockSetting> StockSettings { get; set; }
        public DbSet<CostSetting> CostSettings { get; set; }
        public DbSet<TaxSetting> TaxSettings { get; set; }
        public DbSet<ImportSetting> ImportSettings { get; set; }
        public DbSet<ImportFieldSetting> ImportFieldSettings { get; set; }

        public DbSet<AppSetting> AppSettings { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =========================
            // 🔐 ADMIN USER
            // =========================
            modelBuilder.Entity<AdminUser>()
                .HasIndex(x => x.Username)
                .IsUnique();

            // =========================
            // 📦 PRODUCT
            // =========================
            modelBuilder.Entity<Product>()
                .HasIndex(x => x.Code)
                .IsUnique();

            // YENI:
            // Barkod nullable qalır. SQL Server-də null olmayan barkodlar unique yoxlanılır.
            // Məhsulda barkod yoxdursa null/boş qala bilər.
            modelBuilder.Entity<Product>()
                .HasIndex(x => x.Barcode)
                .IsUnique()
                .HasFilter("[Barcode] IS NOT NULL");

            // YENI:
            // Məhsulun maya/satış qiymətləri üçün precision.
            modelBuilder.Entity<Product>(entity =>
            {
                entity.Property(x => x.PurchasePrice).HasPrecision(18, 2);
                entity.Property(x => x.SalePrice).HasPrecision(18, 2);
                entity.Property(x => x.MinStockQuantity).HasPrecision(18, 3);
                entity.Property(x => x.LastCostPrice).HasPrecision(18, 2);
                entity.Property(x => x.AverageCostPrice).HasPrecision(18, 2);
                // YENI:
                // Məhsulun vergi, çəki və həcm məlumatları üçün precision.
                entity.Property(x => x.VatRate).HasPrecision(18, 4);
                entity.Property(x => x.Weight).HasPrecision(18, 4);
                entity.Property(x => x.Volume).HasPrecision(18, 4);
            });

            modelBuilder.Entity<Product>()
                .HasOne(x => x.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // YENI:
            // Məhsulun dinamik ölçü vahidi ilə əlaqəsi.
            // Product.Unit köhnə string saxlanır, Product.UnitId isə yeni düzgün FK-dir.
            modelBuilder.Entity<Product>()
                .HasOne(x => x.UnitEntity)
                .WithMany(x => x.Products)
                .HasForeignKey(x => x.UnitId)
                .OnDelete(DeleteBehavior.Restrict);
            // =========================
            // 📦 SHELF
            // =========================
            modelBuilder.Entity<Shelf>()
                .HasIndex(x => x.Code)
                .IsUnique();

            modelBuilder.Entity<Shelf>()
                .HasOne(x => x.Warehouse)
                .WithMany(w => w.Shelves)
                .HasForeignKey(x => x.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            // =========================
            // 📦 SHELF STOCK
            // =========================
            modelBuilder.Entity<ShelfStock>()
                .HasOne(x => x.Product)
                .WithMany(p => p.ShelfStocks)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ShelfStock>()
                .HasOne(x => x.Shelf)
                .WithMany(s => s.ShelfStocks)
                .HasForeignKey(x => x.ShelfId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ShelfStock>()
                .HasIndex(x => new { x.ProductId, x.ShelfId })
                .IsUnique();

            // =========================
            // 🧾 INVOICE
            // =========================
            modelBuilder.Entity<Invoice>()
                .HasIndex(x => x.InvoiceNumber)
                .IsUnique();

            modelBuilder.Entity<Invoice>()
                .HasOne(x => x.Supplier)
                .WithMany(s => s.Invoices)
                .HasForeignKey(x => x.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Invoice>()
                .HasOne(x => x.Customer)
                .WithMany(c => c.Invoices)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // YENI:
            // Qaimə məbləğ sahələri üçün precision.
            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.Property(x => x.TotalAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.PaidAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.DebtAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.ItemsTotalAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.ExtraExpenseAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.DiscountAmount)
                    .HasPrecision(18, 2);

                // YENI:
                // Yerli/idxal qaiməsində ƏDV, maya və valyuta toplamları.
                entity.Property(x => x.OriginalItemsTotalAmount).HasPrecision(18, 2);
                entity.Property(x => x.LocalItemsTotalAmount).HasPrecision(18, 2);
                entity.Property(x => x.OriginalPaidAmount).HasPrecision(18, 2);
                entity.Property(x => x.OriginalDebtAmount).HasPrecision(18, 2);
                entity.Property(x => x.NetItemsAmount).HasPrecision(18, 2);
                entity.Property(x => x.VatAmount).HasPrecision(18, 2);
                entity.Property(x => x.GrossItemsAmount).HasPrecision(18, 2);
                entity.Property(x => x.CostIncludedExpenseAmount).HasPrecision(18, 2);
                entity.Property(x => x.CostIncludedTaxAmount).HasPrecision(18, 2);
                entity.Property(x => x.RecoverableTaxAmount).HasPrecision(18, 2);
                entity.Property(x => x.CostExcludedTaxAmount).HasPrecision(18, 2);
                entity.Property(x => x.FinalCostAmount).HasPrecision(18, 2);
                entity.Property(x => x.CostStatus).HasConversion<int>();
            });

            // =========================
            // 🧾 INVOICE ITEM
            // =========================
            modelBuilder.Entity<InvoiceItem>()
                .HasOne(x => x.Invoice)
                .WithMany(i => i.Items)
                .HasForeignKey(x => x.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<InvoiceItem>()
                .HasOne(x => x.Product)
                .WithMany(p => p.InvoiceItems)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InvoiceItem>()
                .HasOne(x => x.Shelf)
                .WithMany()
                .HasForeignKey(x => x.ShelfId)
                .OnDelete(DeleteBehavior.Restrict);

            // =========================
            // 🔄 STOCK MOVEMENT
            // =========================
            modelBuilder.Entity<StockMovement>()
                .HasOne(x => x.Product)
                .WithMany(p => p.StockMovements)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockMovement>()
                .HasOne(x => x.Invoice)
                .WithMany(i => i.StockMovements)
                .HasForeignKey(x => x.InvoiceId)
                .OnDelete(DeleteBehavior.SetNull);

            // =========================
            // 🧩 ATTRIBUTE DEFINITION
            // =========================
            modelBuilder.Entity<AttributeDefinition>()
                .HasOne(x => x.Category)
                .WithMany(x => x.AttributeDefinitions)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AttributeDefinition>()
                .Property(x => x.Name)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<AttributeDefinition>()
                .HasIndex(x => new { x.CategoryId, x.Name })
                .IsUnique();

            // =========================
            // 🧩 ATTRIBUTE VALUE
            // =========================
            modelBuilder.Entity<AttributeValue>()
                .HasOne(x => x.AttributeDefinition)
                .WithMany(x => x.Values)
                .HasForeignKey(x => x.AttributeDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AttributeValue>()
                .Property(x => x.Value)
                .HasMaxLength(250)
                .IsRequired();

            modelBuilder.Entity<AttributeValue>()
                .HasIndex(x => new { x.AttributeDefinitionId, x.Value })
                .IsUnique();

            // =========================
            // 🧩 PRODUCT ATTRIBUTE
            // =========================
            modelBuilder.Entity<ProductAttribute>()
                .HasOne(x => x.Product)
                .WithMany(x => x.Attributes)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductAttribute>()
                .HasOne(x => x.AttributeDefinition)
                .WithMany()
                .HasForeignKey(x => x.AttributeDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductAttribute>()
                .HasOne(x => x.AttributeValue)
                .WithMany(x => x.ProductAttributes)
                .HasForeignKey(x => x.AttributeValueId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductAttribute>()
                .HasIndex(x => new { x.ProductId, x.AttributeDefinitionId })
                .IsUnique();

            // =========================
            // 🧩 SHELF ATTRIBUTE DEFINITION
            // =========================
            modelBuilder.Entity<ShelfAttributeDefinition>()
                .Property(x => x.Name)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<ShelfAttributeDefinition>()
                .Property(x => x.Key)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<ShelfAttributeDefinition>()
                .HasIndex(x => x.Key)
                .IsUnique();

            // =========================
            // 🧩 SHELF ATTRIBUTE VALUE
            // =========================
            modelBuilder.Entity<ShelfAttributeValue>()
                .HasOne(x => x.Shelf)
                .WithMany(x => x.AttributeValues)
                .HasForeignKey(x => x.ShelfId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ShelfAttributeValue>()
                .HasOne(x => x.ShelfAttributeDefinition)
                .WithMany(x => x.Values)
                .HasForeignKey(x => x.ShelfAttributeDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ShelfAttributeValue>()
                .HasIndex(x => new { x.ShelfId, x.ShelfAttributeDefinitionId })
                .IsUnique();

            // =========================
            // 👥 SUPPLIER
            // =========================
            // YENI:
            // Supplier DebtAmount precision.
            modelBuilder.Entity<Supplier>()
                .Property(x => x.DebtAmount)
                .HasPrecision(18, 2);
            // YENI:

            // YENI:

            modelBuilder.Entity<StockBatch>(entity =>
            {
                entity.Property(x => x.BatchNumber)
    .HasMaxLength(50)
    .IsRequired();

                entity.HasIndex(x => x.BatchNumber)
                    .IsUnique();
                entity.Property(x => x.Note)
    .HasMaxLength(500);
                entity.HasOne(x => x.Product)
     .WithMany(x => x.StockBatches)
     .HasForeignKey(x => x.ProductId)
     .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Shelf)
                    .WithMany()
                    .HasForeignKey(x => x.ShelfId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.SourceInvoice)
                    .WithMany(x => x.StockBatches)
                    .HasForeignKey(x => x.SourceInvoiceId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.SourceInvoiceItem)
                    .WithMany()
                    .HasForeignKey(x => x.SourceInvoiceItemId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(x => x.PurchasePrice)
                    .HasPrecision(18, 2);

                entity.Property(x => x.InitialQuantity)
                    .HasPrecision(18, 3);

                entity.Property(x => x.RemainingQuantity)
                    .HasPrecision(18, 3);

                entity.HasIndex(x => new { x.ProductId, x.ShelfId, x.EntryDate });

                entity.HasIndex(x => new { x.ProductId, x.ShelfId, x.RemainingQuantity });
            });
            modelBuilder.Entity<StockMovement>()
                .HasOne(x => x.StockBatch)
                .WithMany(x => x.StockMovements)
                .HasForeignKey(x => x.StockBatchId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Invoice>()
                .HasOne(x => x.ParentInvoice)
                .WithMany(x => x.ReturnInvoices)
                .HasForeignKey(x => x.ParentInvoiceId)
                .OnDelete(DeleteBehavior.Restrict);
            // =========================
            // 👥 SUPPLIER PAYMENT
            // =========================
            // YENI:
            // SupplierPayment konfiqurasiyası.
            modelBuilder.Entity<SupplierPayment>(entity =>
            {
                entity.Property(x => x.Amount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.DebtBeforePayment)
                    .HasPrecision(18, 2);

                entity.Property(x => x.DebtAfterPayment)
                    .HasPrecision(18, 2);

                entity.Property(x => x.Note)
                    .HasMaxLength(500);

                entity.HasOne(x => x.Supplier)
                    .WithMany(x => x.SupplierPayments)
                    .HasForeignKey(x => x.SupplierId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // =========================
            // 💰 EXPENSE TYPE
            // =========================
            // YENI:
            // Xərc növləri: Daşınma, Fəhlə pulu, Endirim və s.
            modelBuilder.Entity<ExpenseType>(entity =>
            {
                entity.Property(x => x.Name)
                    .HasMaxLength(150)
                    .IsRequired();

                // YENI:
                // Xərc növü üçün stabil kod. Məsələn DASIMA, FEHLE, GOMRUK.
                entity.Property(x => x.Code)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.HasIndex(x => x.Name)
                    .IsUnique();

                entity.HasIndex(x => x.Code)
                    .IsUnique();

                entity.Property(x => x.DefaultDirection)
                    .HasConversion<int>();

                entity.HasMany(x => x.FieldDefinitions)
                    .WithOne(x => x.ExpenseType)
                    .HasForeignKey(x => x.ExpenseTypeId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(x => x.InvoiceExpenses)
                    .WithOne(x => x.ExpenseType)
                    .HasForeignKey(x => x.ExpenseTypeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // =========================
            // 💰 EXPENSE TYPE FIELD DEFINITION
            // =========================
            // YENI:
            // Hər xərc növünün default detail sahələri.
            modelBuilder.Entity<ExpenseTypeFieldDefinition>(entity =>
            {
                entity.Property(x => x.FieldKey)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.Label)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(x => x.FieldType)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.HasIndex(x => new { x.ExpenseTypeId, x.FieldKey })
                    .IsUnique();
            });

            // =========================
            // 💰 INVOICE EXPENSE
            // =========================
            // YENI:
            // Qaiməyə əlavə olunan real xərc sətirləri.
            modelBuilder.Entity<InvoiceExpense>(entity =>
            {
                entity.Property(x => x.Name)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(x => x.Amount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.Direction)
                    .HasConversion<int>();

                entity.Property(x => x.Note)
                    .HasMaxLength(500);

                entity.HasOne(x => x.Invoice)
                    .WithMany(x => x.Expenses)
                    .HasForeignKey(x => x.InvoiceId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.ExpenseType)
                    .WithMany(x => x.InvoiceExpenses)
                    .HasForeignKey(x => x.ExpenseTypeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            // =========================
            // 📏 UNIT
            // =========================
            // YENI:
            // Ölçü vahidinin əsas konfiqurasiyası.

            modelBuilder.Entity<LocalPurchaseSetting>()
    .HasIndex(x => x.Code)
    .IsUnique();

            modelBuilder.Entity<LocalPurchaseSettingValue>()
                .HasOne(x => x.LocalPurchaseSetting)
                .WithMany(x => x.Values)
                .HasForeignKey(x => x.LocalPurchaseSettingId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LocalPurchaseSettingValue>()
                .HasIndex(x => new { x.LocalPurchaseSettingId, x.Key })
                .IsUnique();

            modelBuilder.Entity<LocalPurchaseSettingValue>()
                .Property(x => x.Key)
                .HasMaxLength(150);

            modelBuilder.Entity<LocalPurchaseSettingValue>()
                .Property(x => x.Value)
                .HasMaxLength(1000);

            modelBuilder.Entity<LocalPurchaseSettingValue>()
                .Property(x => x.ValueType)
                .HasMaxLength(50);
            modelBuilder.Entity<Unit>(entity =>
            {
                entity.Property(x => x.Key)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.Name)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.Symbol)
                    .HasMaxLength(20)
                    .IsRequired();

                entity.HasIndex(x => x.Key)
                    .IsUnique();

                entity.HasIndex(x => x.Name)
                    .IsUnique();
            });
            // =========================
            // 📂 CATEGORY
            // =========================
            // YENI:
            // Kateqoriyanın default ölçü vahidi ilə əlaqəsi.
            // Bu, məhsul yaradanda avtomatik vahid təklif etmək üçündür.
            modelBuilder.Entity<Category>()
                .HasOne(x => x.DefaultUnit)
                .WithMany(x => x.Categories)
                .HasForeignKey(x => x.DefaultUnitId)
                .OnDelete(DeleteBehavior.Restrict);
            // =========================
            // 💰 INVOICE EXPENSE FIELD VALUE
            // =========================
            // YENI:
            // Qaimə xərclərinin detail key-value məlumatları.
            modelBuilder.Entity<InvoiceExpenseFieldValue>(entity =>
            {
                entity.Property(x => x.FieldKey)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.Label)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(x => x.Value)
                    .HasMaxLength(500);

                entity.HasOne(x => x.InvoiceExpense)
                    .WithMany(x => x.FieldValues)
                    .HasForeignKey(x => x.InvoiceExpenseId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.ExpenseTypeFieldDefinition)
                    .WithMany()
                    .HasForeignKey(x => x.ExpenseTypeFieldDefinitionId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => new { x.InvoiceExpenseId, x.FieldKey })
                    .IsUnique();
            });

            // =========================
            // 🌍 INVOICE IMPORT INFO
            // =========================
            // YENI:
            // İdxal qaiməsinin gömrük, ölkə, valyuta və məzənnə məlumatları.
            modelBuilder.Entity<InvoiceImportInfo>(entity =>
            {
                // Invoice relation
                entity.HasOne(x => x.Invoice)
                    .WithOne(x => x.ImportInfo)
                    .HasForeignKey<InvoiceImportInfo>(x => x.InvoiceId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Declaration
                entity.Property(x => x.DeclarationNumber)
                    .HasMaxLength(100);

                entity.Property(x => x.DeclarationDate);

                // Import info
                entity.Property(x => x.ImportDate);

                entity.Property(x => x.OriginCountry)
                    .HasMaxLength(100);

                entity.Property(x => x.ImportCountry)
                    .HasMaxLength(100);

                entity.Property(x => x.CustomsPoint)
                    .HasMaxLength(200);

                // Foreign supplier
                entity.Property(x => x.ForeignSupplierName)
                    .HasMaxLength(300);

                entity.Property(x => x.ForeignSupplierTaxNumber)
                    .HasMaxLength(100);

                // Foreign invoice
                entity.Property(x => x.ForeignInvoiceNumber)
                    .HasMaxLength(100);

                entity.Property(x => x.ForeignInvoiceDate);

                // Logistics
                entity.Property(x => x.TransportDocumentNumber)
                    .HasMaxLength(200);

                entity.Property(x => x.ContainerNumber)
                    .HasMaxLength(100);

                // Customs
                entity.Property(x => x.HsCode)
                    .HasMaxLength(100);

                entity.Property(x => x.Incoterm)
                    .HasMaxLength(50);

                // Currency
                entity.Property(x => x.Currency)
                    .HasConversion<int>();

                entity.Property(x => x.ExchangeRate)
                    .HasPrecision(18, 6);

                entity.Property(x => x.CurrencyRateSource)
                    .HasConversion<int>();

                // Customs amount
                entity.Property(x => x.CustomsDeclaredAmount)
                    .HasPrecision(18, 2);

                // Note
                entity.Property(x => x.Note)
                    .HasMaxLength(1000);

                // YENI:
                // 1 invoice = 1 import info
                entity.HasIndex(x => x.InvoiceId)
                    .IsUnique();
            });
            // YENI:
            // Customer -> CustomerPayment
            modelBuilder.Entity<CustomerPayment>()
                .HasOne(x => x.Customer)
                .WithMany(x => x.Payments)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // YENI:
            // Customer -> CustomerBalanceTransaction
            modelBuilder.Entity<CustomerBalanceTransaction>()
                .HasOne(x => x.Customer)
                .WithMany(x => x.BalanceTransactions)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // YENI:
            // CustomerPayment -> CustomerBalanceTransaction
            modelBuilder.Entity<CustomerBalanceTransaction>()
                .HasOne(x => x.CustomerPayment)
                .WithMany(x => x.BalanceTransactions)
                .HasForeignKey(x => x.CustomerPaymentId)
                .OnDelete(DeleteBehavior.Restrict);
            // =========================
            // 💳 INVOICE PAYMENT
            // =========================
            // YENI:
            // Qaimənin nağd/kart/bank/kredit ödənişləri ayrıca tarixçəli saxlanır.
            modelBuilder.Entity<InvoicePayment>(entity =>
            {
                entity.HasOne(x => x.Invoice)
                    .WithMany(x => x.Payments)
                    .HasForeignKey(x => x.InvoiceId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(x => x.PaymentType)
                    .HasConversion<int>();

                entity.Property(x => x.Currency)
                    .HasConversion<int>();

                entity.Property(x => x.ExchangeRate)
                    .HasPrecision(18, 6);

                entity.Property(x => x.OriginalAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.LocalAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.ReferenceNumber)
                    .HasMaxLength(100);

                entity.Property(x => x.Note)
                    .HasMaxLength(500);

                entity.HasIndex(x => new { x.InvoiceId, x.PaymentDate });
            });

            // =========================
            // 🧾 INVOICE TAX
            // =========================
            // YENI:
            // Vergi hələ tam hesablanmasa da, ƏDV/idxal ƏDV-si/gömrük rüsumu üçün struktur hazır qalır.
            modelBuilder.Entity<InvoiceTax>(entity =>
            {
                entity.HasOne(x => x.Invoice)
                    .WithMany(x => x.Taxes)
                    .HasForeignKey(x => x.InvoiceId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(x => x.TaxType)
                    .HasConversion<int>();

                entity.Property(x => x.CalculationSource)
                    .HasConversion<int>();

                entity.Property(x => x.CostTreatment)
                    .HasConversion<int>();

                entity.Property(x => x.AllocationMethod)
                    .HasConversion<int>();

                entity.Property(x => x.TaxName)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(x => x.RatePercent)
                    .HasPrecision(18, 4);

                entity.Property(x => x.TaxBaseAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.TaxAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.Currency)
                    .HasConversion<int>();

                entity.Property(x => x.ExchangeRate)
                    .HasPrecision(18, 6);

                entity.Property(x => x.LocalTaxAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.Note)
                    .HasMaxLength(500);

                entity.HasIndex(x => new { x.InvoiceId, x.TaxType });
            });

            // =========================
            // 💰 INVOICE EXPENSE ALLOCATION
            // =========================
            // YENI:
            // Hər xərcin hansı qaimə item-inə nə qədər paylandığını saxlayır.
            modelBuilder.Entity<InvoiceExpenseAllocation>(entity =>
            {
                entity.HasOne(x => x.InvoiceExpense)
                    .WithMany(x => x.Allocations)
                    .HasForeignKey(x => x.InvoiceExpenseId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.InvoiceItem)
                    .WithMany(x => x.ExpenseAllocations)
                    .HasForeignKey(x => x.InvoiceItemId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Product)
                    .WithMany()
                    .HasForeignKey(x => x.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(x => x.AllocatedAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.UnitAllocatedAmount)
                    .HasPrecision(18, 4);

                entity.Property(x => x.Note)
                    .HasMaxLength(500);

                entity.HasIndex(x => new { x.InvoiceExpenseId, x.InvoiceItemId })
                    .IsUnique();
            });

            // =========================
            // 👥 SUPPLIER BALANCE TRANSACTION
            // =========================
            // YENI:
            // Təchizatçı borcunun qaimə/ödəniş üzrə tarixçəsi.
            modelBuilder.Entity<SupplierBalanceTransaction>(entity =>
            {
                entity.HasOne(x => x.Supplier)
                    .WithMany(x => x.BalanceTransactions)
                    .HasForeignKey(x => x.SupplierId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Invoice)
                    .WithMany(x => x.SupplierBalanceTransactions)
                    .HasForeignKey(x => x.InvoiceId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(x => x.SupplierPayment)
                    .WithMany(x => x.BalanceTransactions)
                    .HasForeignKey(x => x.SupplierPaymentId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.Property(x => x.TransactionType)
                    .HasConversion<int>();

                entity.Property(x => x.Currency)
                    .HasConversion<int>();

                entity.Property(x => x.ExchangeRate)
                    .HasPrecision(18, 6);

                entity.Property(x => x.OriginalAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.LocalAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.DebtBefore)
                    .HasPrecision(18, 2);

                entity.Property(x => x.DebtAfter)
                    .HasPrecision(18, 2);

                entity.Property(x => x.Note)
                    .HasMaxLength(500);

                entity.HasIndex(x => new { x.SupplierId, x.TransactionDate });
            });

            // =========================
            // 👤 CUSTOMER BALANCE TRANSACTION
            // =========================
            // YENI:
            // Müştəri borcu üçün tarixçəli struktur.
            modelBuilder.Entity<CustomerBalanceTransaction>(entity =>
            {
                entity.HasOne(x => x.Customer)
                    .WithMany(x => x.BalanceTransactions)
                    .HasForeignKey(x => x.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Invoice)
                    .WithMany(x => x.CustomerBalanceTransactions)
                    .HasForeignKey(x => x.InvoiceId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.Property(x => x.TransactionType)
                    .HasConversion<int>();

                entity.Property(x => x.Currency)
                    .HasConversion<int>();

                entity.Property(x => x.ExchangeRate)
                    .HasPrecision(18, 6);

                entity.Property(x => x.OriginalAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.LocalAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.DebtBefore)
                    .HasPrecision(18, 2);

                entity.Property(x => x.DebtAfter)
                    .HasPrecision(18, 2);

                entity.Property(x => x.Note)
                    .HasMaxLength(500);

                entity.HasIndex(x => new { x.CustomerId, x.TransactionDate });
            });

            // =========================
            // 💱 CURRENCY RATE
            // =========================
            // YENI:
            // Valyuta məzənnələri tarixə görə saxlanır.
            modelBuilder.Entity<CurrencyRate>(entity =>
            {
                entity.Property(x => x.FromCurrency)
                    .HasConversion<int>();

                entity.Property(x => x.ToCurrency)
                    .HasConversion<int>();

                entity.Property(x => x.Source)
                    .HasConversion<int>();

                entity.Property(x => x.Rate)
                    .HasPrecision(18, 6);

                entity.Property(x => x.Note)
                    .HasMaxLength(500);

                entity.HasIndex(x => new { x.FromCurrency, x.ToCurrency, x.RateDate })
                    .IsUnique();
            });

            // =========================
            // ⚙️ ENTERPRISE SETTINGS
            // =========================
            // YENI:
            // Ayarlar bölməsində saxlanacaq əsas tək-sətirli konfiqurasiyalar.
            modelBuilder.Entity<WarehouseSetting>(entity =>
            {
                entity.Property(x => x.AppName).HasMaxLength(150).IsRequired();
                entity.Property(x => x.CompanyName).HasMaxLength(200);
                entity.Property(x => x.CompanyVoen).HasMaxLength(50);
                entity.Property(x => x.CompanyPhone).HasMaxLength(50);
                entity.Property(x => x.CompanyAddress).HasMaxLength(300);
                entity.Property(x => x.DefaultCurrency).HasConversion<int>();
            });

            modelBuilder.Entity<InvoiceSetting>(entity =>
            {
                entity.Property(x => x.InvoicePrefix).HasMaxLength(20).IsRequired();
            });

            modelBuilder.Entity<StockSetting>(entity =>
            {
                // Hal-hazırda sadəcə bool ayarlardır, relation yoxdur.
            });

            modelBuilder.Entity<CostSetting>(entity =>
            {
                entity.Property(x => x.DefaultAllocationMethod).HasConversion<int>();
                entity.Property(x => x.MinimumMarginPercent).HasPrecision(18, 4);
            });

            modelBuilder.Entity<TaxSetting>(entity =>
            {
                entity.Property(x => x.TaxRegime).HasConversion<int>();
                entity.Property(x => x.VATPercent).HasPrecision(18, 4);
                entity.Property(x => x.ProfitTaxPercent).HasPrecision(18, 4);
                entity.Property(x => x.SimplifiedTaxPercent).HasPrecision(18, 4);
            });

            modelBuilder.Entity<ImportSetting>(entity =>
            {
                // Hal-hazırda sadəcə idxal davranış ayarlarıdır, relation yoxdur.
            });

            modelBuilder.Entity<ImportFieldSetting>(entity =>
            {
                entity.Property(x => x.FieldKey).HasMaxLength(100).IsRequired();
                entity.Property(x => x.DisplayName).HasMaxLength(150).IsRequired();
                entity.Property(x => x.FieldType).HasConversion<int>();
                entity.Property(x => x.OptionsJson).HasMaxLength(1000);
                entity.Property(x => x.DefaultValue).HasMaxLength(500);
                entity.Property(x => x.Placeholder).HasMaxLength(250);
                entity.HasIndex(x => x.FieldKey).IsUnique();
            });

            // =========================
            // YENI:
            // Mövcud entity-lərdə yeni maliyyə/valyuta/maya sahələri üçün əlavə konfiqurasiyalar.
            // =========================
            modelBuilder.Entity<Supplier>(entity =>
            {
                entity.Property(x => x.DefaultCurrency).HasConversion<int>();
                entity.Property(x => x.OriginType).HasConversion<int>();
                entity.Property(x => x.CreditLimit).HasPrecision(18, 2);
                entity.Property(x => x.DebtAmountLocal).HasPrecision(18, 2);
                entity.Property(x => x.DebtAmountOriginal).HasPrecision(18, 2);
                entity.Property(x => x.Country).HasMaxLength(100);
            });

            modelBuilder.Entity<Customer>(entity =>
            {
                entity.Property(x => x.DebtAmountLocal).HasPrecision(18, 2);
                entity.Property(x => x.DebtAmountOriginal).HasPrecision(18, 2);
            });

            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.Property(x => x.Currency)
                    .HasConversion<int>();

                entity.Property(x => x.ExchangeRate)
                    .HasPrecision(18, 6);

                entity.Property(x => x.OriginalItemsTotalAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.LocalItemsTotalAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.OriginalTotalAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.LocalTotalAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.TotalAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.PaidAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.DebtAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.OriginalPaidAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.OriginalDebtAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.ItemsTotalAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.ExtraExpenseAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.DiscountAmount)
                    .HasPrecision(18, 2);

                // YENI:
                // Enterprise adlar.
                entity.Property(x => x.NetItemsAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.GrossItemsAmount)
                    .HasPrecision(18, 2);

                // YENI:
                // Köhnə kod compatibility adları.
                entity.Property(x => x.NetAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.GrossAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.VatAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.CostIncludedExpenseAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.CostIncludedTaxAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.RecoverableTaxAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.CostExcludedTaxAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.FinalCostAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.CostStatus)
                    .HasConversion<int>();

                entity.Property(x => x.PaymentStatus)
                    .HasConversion<int>();

                entity.Property(x => x.PaymentType)
                    .HasConversion<int>();

                entity.Property(x => x.Note)
                    .HasMaxLength(1000);
            });

            modelBuilder.Entity<InvoiceItem>(entity =>
            {
                entity.Property(x => x.ProductBarcode).HasMaxLength(100);
                entity.Property(x => x.ProductNameSnapshot).HasMaxLength(250);
                entity.Property(x => x.ProductCodeSnapshot).HasMaxLength(100);

                entity.Property(x => x.Quantity).HasPrecision(18, 3);
                entity.Property(x => x.Price).HasPrecision(18, 2);
                entity.Property(x => x.Total).HasPrecision(18, 2);

                entity.Property(x => x.Currency).HasConversion<int>();
                entity.Property(x => x.ExchangeRate).HasPrecision(18, 6);

                entity.Property(x => x.OriginalUnitPrice).HasPrecision(18, 2);
                entity.Property(x => x.OriginalTotalAmount).HasPrecision(18, 2);
                entity.Property(x => x.LocalUnitPrice).HasPrecision(18, 2);
                entity.Property(x => x.LocalTotalAmount).HasPrecision(18, 2);

                entity.Property(x => x.NetAmount).HasPrecision(18, 2);
                entity.Property(x => x.VatAmount).HasPrecision(18, 2);
                entity.Property(x => x.GrossAmount).HasPrecision(18, 2);

                entity.Property(x => x.DiscountPercent).HasPrecision(18, 4);
                entity.Property(x => x.DiscountAmount).HasPrecision(18, 2);

                entity.Property(x => x.ExpenseUnitShare).HasPrecision(18, 4);
                entity.Property(x => x.TaxUnitShare).HasPrecision(18, 4);
                entity.Property(x => x.DiscountUnitShare).HasPrecision(18, 4);
                entity.Property(x => x.FinalUnitCost).HasPrecision(18, 4);
                entity.Property(x => x.FinalTotalCost).HasPrecision(18, 2);
            });

            modelBuilder.Entity<InvoiceExpense>(entity =>
            {
                entity.Property(x => x.Currency).HasConversion<int>();
                entity.Property(x => x.ExchangeRate).HasPrecision(18, 6);
                entity.Property(x => x.OriginalAmount).HasPrecision(18, 2);
                entity.Property(x => x.LocalAmount).HasPrecision(18, 2);
                entity.Property(x => x.AllocationMethod).HasConversion<int>();
                // YENI:
                // Enterprise xərc davranışı üçün əlavə field-lər sadə bool olduğu üçün ayrıca conversion lazım deyil.
            });

            modelBuilder.Entity<ExpenseType>(entity =>
            {
                entity.Property(x => x.DefaultAllocationMethod).HasConversion<int>();
            });
            // =========================
            // 🧾 TAX CATALOG
            // =========================
            // YENI:
            // Vergi kataloqu: ƏDV, Gömrük rüsumu, Aksiz və s.
            modelBuilder.Entity<Tax>(entity =>
            {
                entity.Property(x => x.Name)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(x => x.Code)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.HasIndex(x => x.Code)
                    .IsUnique();

                entity.Property(x => x.TaxType)
                    .HasConversion<int>();

                entity.Property(x => x.DefaultRatePercent)
                    .HasPrecision(18, 4);

                entity.Property(x => x.DefaultCalculationSource)
                    .HasConversion<int>();

                entity.Property(x => x.DefaultCostTreatment)
                    .HasConversion<int>();

                entity.Property(x => x.Note)
                    .HasMaxLength(500);
            });

            // =========================
            // 🧾 PRODUCT TAX
            // =========================
            // YENI:
            // Məhsulun default vergi qaydaları.
            modelBuilder.Entity<ProductTax>(entity =>
            {
                entity.HasOne(x => x.Product)
                    .WithMany(x => x.ProductTaxes)
                    .HasForeignKey(x => x.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Tax)
                    .WithMany(x => x.ProductTaxes)
                    .HasForeignKey(x => x.TaxId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(x => x.TaxType)
                    .HasConversion<int>();

                entity.Property(x => x.RatePercent)
                    .HasPrecision(18, 4);

                entity.Property(x => x.CostTreatment)
                    .HasConversion<int>();

                entity.Property(x => x.Note)
                    .HasMaxLength(500);

                entity.HasIndex(x => new { x.ProductId, x.TaxId })
                    .IsUnique();
            });

            // =========================
            // 🧾 INVOICE ITEM TAX
            // =========================
            // YENI:
            // Qaimə item-in real vergi snapshot-ları.
            modelBuilder.Entity<InvoiceItemTax>(entity =>
            {
                entity.HasOne(x => x.InvoiceItem)
                    .WithMany(x => x.Taxes)
                    .HasForeignKey(x => x.InvoiceItemId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Invoice)
                    .WithMany(x => x.ItemTaxes)
                    .HasForeignKey(x => x.InvoiceId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Product)
                    .WithMany()
                    .HasForeignKey(x => x.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Tax)
                    .WithMany(x => x.InvoiceItemTaxes)
                    .HasForeignKey(x => x.TaxId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(x => x.TaxType)
                    .HasConversion<int>();

                entity.Property(x => x.TaxName)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(x => x.CalculationSource)
                    .HasConversion<int>();

                entity.Property(x => x.CostTreatment)
                    .HasConversion<int>();

                entity.Property(x => x.RatePercent)
                    .HasPrecision(18, 4);

                entity.Property(x => x.TaxBaseAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.TaxAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.Currency)
                    .HasConversion<int>();

                entity.Property(x => x.ExchangeRate)
                    .HasPrecision(18, 6);

                entity.Property(x => x.LocalTaxBaseAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.LocalTaxAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.Note)
                    .HasMaxLength(500);

                entity.HasIndex(x => new { x.InvoiceItemId, x.TaxType });
            });
            modelBuilder.Entity<StockBatch>(entity =>
            {
                entity.Property(x => x.PurchaseUnitPrice).HasPrecision(18, 4);
                entity.Property(x => x.OriginalUnitPrice).HasPrecision(18, 4);
                entity.Property(x => x.Currency).HasConversion<int>();
                entity.Property(x => x.ExchangeRate).HasPrecision(18, 6);
                entity.Property(x => x.ExpenseUnitShare).HasPrecision(18, 4);
                entity.Property(x => x.TaxUnitShare).HasPrecision(18, 4);
                entity.Property(x => x.DiscountUnitShare).HasPrecision(18, 4);
                entity.Property(x => x.FinalUnitCost).HasPrecision(18, 4);
                entity.Property(x => x.FinalTotalCost).HasPrecision(18, 2);
            });
            modelBuilder.Entity<Product>()
    .HasOne(x => x.Brand)
    .WithMany(x => x.Products)
    .HasForeignKey(x => x.BrandId)
    .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<StockMovement>(entity =>
            {
                entity.Property(x => x.UnitCost).HasPrecision(18, 4);
                entity.Property(x => x.TotalCost).HasPrecision(18, 2);
                entity.Property(x => x.Currency).HasConversion<int>();
                entity.Property(x => x.ExchangeRate).HasPrecision(18, 6);
            });
            modelBuilder.Entity<Brand>(entity =>
            {
                entity.Property(x => x.Name)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(x => x.Code)
                    .HasMaxLength(50);

                entity.Property(x => x.Country)
                    .HasMaxLength(100);

                entity.Property(x => x.Description)
                    .HasMaxLength(500);

                entity.HasIndex(x => x.Name)
                    .IsUnique();
            });
            // =========================
            // 🧾 INVOICE TAX ALLOCATION
            // =========================
            // YENI:
            // Vergi/rüsumların item-lərə paylanması.
            modelBuilder.Entity<InvoiceTaxAllocation>(entity =>
            {
                entity.HasOne(x => x.InvoiceTax)
                    .WithMany(x => x.Allocations)
                    .HasForeignKey(x => x.InvoiceTaxId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.InvoiceItem)
                    .WithMany(x => x.TaxAllocations)
                    .HasForeignKey(x => x.InvoiceItemId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Product)
                    .WithMany()
                    .HasForeignKey(x => x.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                // YENI:
                // İstəyə bağlı olaraq bu allocation konkret InvoiceItemTax snapshot-a bağlana bilər.
                entity.HasOne(x => x.InvoiceItemTax)
                    .WithMany()
                    .HasForeignKey(x => x.InvoiceItemTaxId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(x => x.AllocatedAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.UnitAllocatedAmount)
                    .HasPrecision(18, 4);

                entity.Property(x => x.Note)
                    .HasMaxLength(500);

                entity.HasIndex(x => new { x.InvoiceTaxId, x.InvoiceItemId })
                    .IsUnique();
            });

            // =========================
            // 🧾 INVOICE COST SUMMARY
            // =========================
            // YENI:
            // Qaimənin maya dəyəri yekun cədvəli.
            modelBuilder.Entity<InvoiceCostSummary>(entity =>
            {
                entity.HasOne(x => x.Invoice)
                    .WithOne(x => x.CostSummary)
                    .HasForeignKey<InvoiceCostSummary>(x => x.InvoiceId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(x => x.BaseItemsAmount).HasPrecision(18, 2);
                entity.Property(x => x.NetItemsAmount).HasPrecision(18, 2);
                entity.Property(x => x.VatAmount).HasPrecision(18, 2);
                entity.Property(x => x.GrossItemsAmount).HasPrecision(18, 2);
                entity.Property(x => x.CostIncludedExpenseAmount).HasPrecision(18, 2);
                entity.Property(x => x.CostExcludedExpenseAmount).HasPrecision(18, 2);
                entity.Property(x => x.CostIncludedTaxAmount).HasPrecision(18, 2);
                entity.Property(x => x.RecoverableTaxAmount).HasPrecision(18, 2);
                entity.Property(x => x.CostExcludedTaxAmount).HasPrecision(18, 2);
                entity.Property(x => x.DiscountAmount).HasPrecision(18, 2);
                entity.Property(x => x.FinalCostAmount).HasPrecision(18, 2);
                entity.Property(x => x.Note).HasMaxLength(500);

                entity.HasIndex(x => x.InvoiceId)
                    .IsUnique();
            });

            // =========================
            // 🧩 INVOICE DYNAMIC FIELD VALUE
            // =========================
            // YENI:
            // İdxal/qaimə dynamic field dəyərləri.
            modelBuilder.Entity<InvoiceDynamicFieldValue>(entity =>
            {
                entity.HasOne(x => x.Invoice)
                    .WithMany(x => x.DynamicFieldValues)
                    .HasForeignKey(x => x.InvoiceId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(x => x.FieldKey).HasMaxLength(100).IsRequired();
                entity.Property(x => x.FieldName).HasMaxLength(150).IsRequired();
                entity.Property(x => x.FieldType).HasConversion<int>();
                entity.Property(x => x.Value).HasMaxLength(1000);
                entity.Property(x => x.Note).HasMaxLength(500);

                entity.HasIndex(x => new { x.InvoiceId, x.FieldKey })
                    .IsUnique();
            });

            // =========================
            // 🌱 DEFAULT EXPENSE TYPES
            // =========================
            // YENI:
            // Sistem ilk açılarkən əsas xərc növləri hazır gəlsin.
            modelBuilder.Entity<ExpenseType>().HasData(
                new ExpenseType
                {
                    Id = 1,
                    Name = "Daşınma",
                    Code = "DASIMA",
                    DefaultDirection = ExpenseDirection.Plus,
                    IsSystem = true,
                    UseForStockIn = true,
                    UseForStockOut = true,
                    AffectStockCost = true,
                    IsActive = true
                },
                new ExpenseType
                {
                    Id = 2,
                    Name = "Fəhlə pulu",
                    Code = "FEHLE",
                    DefaultDirection = ExpenseDirection.Plus,
                    IsSystem = true,
                    UseForStockIn = true,
                    UseForStockOut = true,
                    AffectStockCost = true,
                    IsActive = true
                },
                new ExpenseType
                {
                    Id = 3,
                    Name = "Endirim",
                    Code = "ENDIRIM",
                    DefaultDirection = ExpenseDirection.Minus,
                    IsSystem = true,
                    UseForStockIn = true,
                    UseForStockOut = true,
                    AffectStockCost = false,
                    IsActive = true
                },
                new ExpenseType
                {
                    Id = 4,
                    Name = "Digər xərc",
                    Code = "DIGER",
                    DefaultDirection = ExpenseDirection.Plus,
                    IsSystem = true,
                    UseForStockIn = true,
                    UseForStockOut = true,
                    AffectStockCost = false,
                    IsActive = true
                }
            );
            // 🌱 DEFAULT UNITS
            // YENI:
            // Sistem ilk migration zamanı əsas ölçü vahidləri ilə dolsun.
            modelBuilder.Entity<Unit>().HasData(
                new Unit
                {
                    Id = 1,
                    Key = "eded",
                    Name = "Ədəd",
                    Symbol = "əd",
                    SortOrder = 1,
                    IsDefault = true,
                    IsActive = true
                },
                new Unit
                {
                    Id = 2,
                    Key = "kg",
                    Name = "Kiloqram",
                    Symbol = "kq",
                    SortOrder = 2,
                    IsDefault = false,
                    IsActive = true
                },
                new Unit
                {
                    Id = 3,
                    Key = "qram",
                    Name = "Qram",
                    Symbol = "qr",
                    SortOrder = 3,
                    IsDefault = false,
                    IsActive = true
                },
                new Unit
                {
                    Id = 4,
                    Key = "litr",
                    Name = "Litr",
                    Symbol = "l",
                    SortOrder = 4,
                    IsDefault = false,
                    IsActive = true
                },
                new Unit
                {
                    Id = 5,
                    Key = "metr",
                    Name = "Metr",
                    Symbol = "m",
                    SortOrder = 5,
                    IsDefault = false,
                    IsActive = true
                },
                new Unit
                {
                    Id = 6,
                    Key = "m2",
                    Name = "Kvadrat metr",
                    Symbol = "m²",
                    SortOrder = 6,
                    IsDefault = false,
                    IsActive = true
                },
                new Unit
                {
                    Id = 7,
                    Key = "m3",
                    Name = "Kub metr",
                    Symbol = "m³",
                    SortOrder = 7,
                    IsDefault = false,
                    IsActive = true
                },
                new Unit
                {
                    Id = 8,
                    Key = "qutu",
                    Name = "Qutu",
                    Symbol = "qutu",
                    SortOrder = 8,
                    IsDefault = false,
                    IsActive = true
                },
                new Unit
                {
                    Id = 9,
                    Key = "paket",
                    Name = "Paket",
                    Symbol = "pkt",
                    SortOrder = 9,
                    IsDefault = false,
                    IsActive = true
                }
            );
            modelBuilder.Entity<Tax>().HasData(
    new Tax
    {
        Id = 1,
        Name = "ƏDV",
        Code = "VAT",
        TaxType = TaxType.VAT,
        DefaultRatePercent = 18,
        IsRecoverableByDefault = true,
        IsIncludedInCostByDefault = false,
        IsIncludedInPriceByDefault = true,
        DefaultCalculationSource = TaxCalculationSource.Product,
        DefaultCostTreatment = TaxCostTreatment.Recoverable,
        UseForLocalPurchase = true,
        UseForImportPurchase = true,
        UseForSale = true,
        IsActive = true
    },
    new Tax
    {
        Id = 2,
        Name = "Gömrük rüsumu",
        Code = "CUSTOMS_DUTY",
        TaxType = TaxType.CustomsDuty,
        DefaultRatePercent = 0,
        IsRecoverableByDefault = false,
        IsIncludedInCostByDefault = true,
        IsIncludedInPriceByDefault = false,
        DefaultCalculationSource = TaxCalculationSource.Import,
        DefaultCostTreatment = TaxCostTreatment.IncludedInCost,
        UseForLocalPurchase = false,
        UseForImportPurchase = true,
        UseForSale = false,
        IsActive = true
    },
    new Tax
    {
        Id = 3,
        Name = "Aksiz",
        Code = "EXCISE",
        TaxType = TaxType.Excise,
        DefaultRatePercent = 0,
        IsRecoverableByDefault = false,
        IsIncludedInCostByDefault = true,
        IsIncludedInPriceByDefault = false,
        DefaultCalculationSource = TaxCalculationSource.Import,
        DefaultCostTreatment = TaxCostTreatment.IncludedInCost,
        UseForLocalPurchase = true,
        UseForImportPurchase = true,
        UseForSale = false,
        IsActive = true
    }
);
            // YENI:
            // Daşınma və Fəhlə pulu üçün default detail sahələri.
            modelBuilder.Entity<ExpenseTypeFieldDefinition>().HasData(
                new ExpenseTypeFieldDefinition
                {
                    Id = 1,
                    ExpenseTypeId = 1,
                    FieldKey = "DriverName",
                    Label = "Sürücü adı",
                    FieldType = "Text",
                    IsRequired = false,
                    SortOrder = 1,
                    IsActive = true
                },
                new ExpenseTypeFieldDefinition
                {
                    Id = 2,
                    ExpenseTypeId = 1,
                    FieldKey = "DriverPhone",
                    Label = "Telefon",
                    FieldType = "Text",
                    IsRequired = false,
                    SortOrder = 2,
                    IsActive = true
                },
                new ExpenseTypeFieldDefinition
                {
                    Id = 3,
                    ExpenseTypeId = 1,
                    FieldKey = "VehicleName",
                    Label = "Maşın",
                    FieldType = "Text",
                    IsRequired = false,
                    SortOrder = 3,
                    IsActive = true
                },
                new ExpenseTypeFieldDefinition
                {
                    Id = 4,
                    ExpenseTypeId = 1,
                    FieldKey = "VehicleNumber",
                    Label = "Dövlət nömrəsi",
                    FieldType = "Text",
                    IsRequired = false,
                    SortOrder = 4,
                    IsActive = true
                },
                new ExpenseTypeFieldDefinition
                {
                    Id = 5,
                    ExpenseTypeId = 1,
                    FieldKey = "Distance",
                    Label = "Məsafə",
                    FieldType = "Text",
                    IsRequired = false,
                    SortOrder = 5,
                    IsActive = true
                },
                new ExpenseTypeFieldDefinition
                {
                    Id = 6,
                    ExpenseTypeId = 2,
                    FieldKey = "WorkerName",
                    Label = "Fəhlə adı",
                    FieldType = "Text",
                    IsRequired = false,
                    SortOrder = 1,
                    IsActive = true
                },
                new ExpenseTypeFieldDefinition
                {
                    Id = 7,
                    ExpenseTypeId = 2,
                    FieldKey = "WorkerPhone",
                    Label = "Telefon",
                    FieldType = "Text",
                    IsRequired = false,
                    SortOrder = 2,
                    IsActive = true
                },
                new ExpenseTypeFieldDefinition
                {
                    Id = 8,
                    ExpenseTypeId = 2,
                    FieldKey = "WorkType",
                    Label = "İş növü",
                    FieldType = "Text",
                    IsRequired = false,
                    SortOrder = 3,
                    IsActive = true
                },
                new ExpenseTypeFieldDefinition
                {
                    Id = 9,
                    ExpenseTypeId = 2,
                    FieldKey = "WorkHour",
                    Label = "İş saatı",
                    FieldType = "Text",
                    IsRequired = false,
                    SortOrder = 4,
                    IsActive = true
                }
            );

            // =========================
            // 🌱 DEFAULT ENTERPRISE SETTINGS
            // =========================
            // YENI:
            // Ayarlar bölməsi ilk migration zamanı boş qalmasın deyə default sətirlər.
            modelBuilder.Entity<WarehouseSetting>().HasData(
                new WarehouseSetting
                {
                    Id = 1,
                    AppName = "Mebel Anbar Sistemi",
                    DefaultCurrency = CurrencyType.AZN,
                    IsActive = true
                }
            );

            modelBuilder.Entity<InvoiceSetting>().HasData(
                new InvoiceSetting
                {
                    Id = 1,
                    InvoicePrefix = "QAI",
                    LockConfirmedInvoice = true,
                    RequireReturnReason = true,
                    RequireShelfSelection = true,
                    RequireBatchSelectionForReturn = true,
                    CopyProductBarcodeToInvoiceItem = true,
                    IsActive = true
                }
            );

            modelBuilder.Entity<StockSetting>().HasData(
                new StockSetting
                {
                    Id = 1,
                    EnableFIFO = true,
                    PreventNegativeStock = true,
                    CheckShelfCapacity = true,
                    BlockPassiveProductInInvoice = true,
                    AutoCreateBatchOnStockIn = true,
                    IsActive = true
                }
            );

            modelBuilder.Entity<CostSetting>().HasData(
                new CostSetting
                {
                    Id = 1,
                    IncludeExpensesInStockCost = true,
                    DefaultAllocationMethod = CostAllocationMethod.ByAmount,
                    SuggestSalePrice = true,
                    MinimumMarginPercent = 0,
                    AutoCalculateCostOnConfirm = true,
                    RecalculateCostWhenExpenseChanges = true,
                    ExcludeZeroAmountExpenses = true,
                    LockCostAfterConfirm = true,
                    IsActive = true
                }
            );

            modelBuilder.Entity<TaxSetting>().HasData(
                new TaxSetting
                {
                    Id = 1,
                    TaxRegime = TaxRegime.NoTax,
                    EnableVAT = false,
                    VATPercent = 18,
                    EnableProfitTax = false,
                    ProfitTaxPercent = 20,
                    EnableSimplifiedTax = false,
                    SimplifiedTaxPercent = 2,
                    IncludeImportVATInCost = false,
                    PurchasePricesIncludeVATByDefault = true,
                    VATRecoverableByDefault = true,
                    IncludeCustomsDutyInCost = true,
                    IncludeExciseInCost = true,
                    IsActive = true
                }
            );

            modelBuilder.Entity<ImportSetting>().HasData(
                new ImportSetting
                {
                    Id = 1,
                    EnableImportInvoice = true,
                    AutoOpenImportFieldsForForeignSupplier = true,
                    RequireDeclarationNumber = false,
                    RequireExchangeRate = true,
                    UseInvoiceDateExchangeRate = false,
                    IncludeCustomsDutyInCost = true,
                    IncludeBrokerFeeInCost = true,
                    IncludeInsuranceInCost = true,
                    IncludeTransportInCost = true,
                    IsActive = true
                }
            );

            modelBuilder.Entity<ImportFieldSetting>().HasData(
                new ImportFieldSetting { Id = 1, FieldKey = "DeclarationNumber", DisplayName = "Gömrük bəyannamə №", IsVisible = true, IsRequired = false, SortOrder = 1, IsActive = true },
                new ImportFieldSetting { Id = 2, FieldKey = "ImportDate", DisplayName = "İdxal tarixi", IsVisible = true, IsRequired = false, SortOrder = 2, IsActive = true },
                new ImportFieldSetting { Id = 3, FieldKey = "OriginCountry", DisplayName = "Mənşə ölkəsi", IsVisible = true, IsRequired = false, SortOrder = 3, IsActive = true },
                new ImportFieldSetting { Id = 4, FieldKey = "CustomsPoint", DisplayName = "Gömrük postu", IsVisible = true, IsRequired = false, SortOrder = 4, IsActive = true },
                new ImportFieldSetting { Id = 5, FieldKey = "Currency", DisplayName = "Valyuta", IsVisible = true, IsRequired = true, SortOrder = 5, IsActive = true },
                new ImportFieldSetting { Id = 6, FieldKey = "ExchangeRate", DisplayName = "Məzənnə", IsVisible = true, IsRequired = true, SortOrder = 6, IsActive = true },
                new ImportFieldSetting { Id = 7, FieldKey = "ForeignInvoiceNumber", DisplayName = "Xarici qaimə №", IsVisible = true, IsRequired = false, SortOrder = 7, IsActive = true },
                new ImportFieldSetting { Id = 8, FieldKey = "TransportDocumentNumber", DisplayName = "Daşıma sənədi №", IsVisible = true, IsRequired = false, SortOrder = 8, IsActive = true }
            );

        }
    }
}
