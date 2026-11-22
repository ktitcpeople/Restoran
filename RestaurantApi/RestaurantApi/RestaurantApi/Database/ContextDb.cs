using Microsoft.EntityFrameworkCore;
using RestaurantApi.Models;
using System.Security.Claims;

namespace RestaurantApi.Database
{
    public class ContextDb : DbContext
    {
        private readonly IHttpContextAccessor _http;
        public ContextDb(DbContextOptions<ContextDb> options, IHttpContextAccessor http) : base(options) => _http = http;

        public int? CurrentBranchID => int.TryParse(_http.HttpContext?.User.FindFirstValue("branch"), out var id) ? id : null;
        public int CurrentRole => int.TryParse(_http.HttpContext?.User.FindFirstValue("role"), out var role) ? role : 0;
        public bool IsBranchScopedRole => CurrentRole is 1 or 2 or 3;

        public DbSet<User> Users { get; set; }
        public DbSet<RestaurantTable> Tables { get; set; }
        public DbSet<MenuItem> MenuItems { get; set; }
        public DbSet<MenuCategory> MenuCategories { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Ingredient> Ingredients { get; set; }
        public DbSet<TechCard> TechCards { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Delivery> Deliveries { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<SupportConversation> SupportConversations { get; set; }
        public DbSet<SupportMessage> SupportMessages { get; set; }
        public DbSet<SupportTicket> SupportTickets { get; set; }
        public DbSet<KnowledgeArticle> KnowledgeArticles { get; set; }
        public DbSet<SupplyRequest> SupplyRequests { get; set; }
        public DbSet<Stocktake> Stocktakes { get; set; }
        public DbSet<StocktakeItem> StocktakeItems { get; set; }
        public DbSet<StockTransaction> StockTransactions { get; set; }
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
        public DbSet<Branch> Branches { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var restrict = DeleteBehavior.Restrict;
            var setNull = DeleteBehavior.SetNull;

            modelBuilder.Entity<User>()
                .HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId);
            modelBuilder.Entity<User>().HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchID).OnDelete(restrict);
            modelBuilder.Entity<RestaurantTable>().HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchID).OnDelete(restrict);
            modelBuilder.Entity<MenuCategory>().HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchID).OnDelete(restrict);
            modelBuilder.Entity<MenuItem>().HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchID).OnDelete(restrict);
            modelBuilder.Entity<Ingredient>().HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchID).OnDelete(restrict);
            modelBuilder.Entity<Order>().HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchID).OnDelete(restrict);
            modelBuilder.Entity<SupportConversation>().HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchID).OnDelete(restrict);
            modelBuilder.Entity<Stocktake>().HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchID).OnDelete(restrict);
            modelBuilder.Entity<MenuItem>()
                .HasOne(x => x.MenuCategory).WithMany().HasForeignKey(x => x.MenuCategoryID);
            modelBuilder.Entity<Reservation>()
                .HasOne(x => x.Table).WithMany().HasForeignKey(x => x.TableID);
            modelBuilder.Entity<Order>()
                .HasOne(x => x.Table).WithMany().HasForeignKey(x => x.TableID);
            modelBuilder.Entity<Order>()
                .HasOne<User>().WithMany().HasForeignKey(x => x.WaiterID);
            modelBuilder.Entity<Order>()
                .HasOne<User>().WithMany().HasForeignKey(x => x.PreparingByUserID);
            modelBuilder.Entity<Order>()
                .HasOne<User>().WithMany().HasForeignKey(x => x.ReadyByUserID);
            modelBuilder.Entity<Order>()
                .HasOne<User>().WithMany().HasForeignKey(x => x.CustomerUserID).OnDelete(restrict);
            modelBuilder.Entity<Order>()
                .HasIndex(x => new { x.CustomerUserID, x.BirthdayDiscountYear })
                .IsUnique().HasFilter("\"BirthdayDiscountYear\" IS NOT NULL");

            modelBuilder.Entity<OrderItem>()
                .HasOne(x => x.Order).WithMany(x => x.Items).HasForeignKey(x => x.OrderID);
            modelBuilder.Entity<OrderItem>()
                .HasOne(x => x.MenuItem).WithMany().HasForeignKey(x => x.MenuItemID);
            modelBuilder.Entity<Delivery>()
                .HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderID);
            modelBuilder.Entity<Delivery>()
                .HasOne(x => x.Courier).WithMany().HasForeignKey(x => x.CourierID);
            modelBuilder.Entity<TechCard>()
                .HasOne(x => x.MenuItem).WithMany().HasForeignKey(x => x.MenuItemID);
            modelBuilder.Entity<TechCard>()
                .HasOne(x => x.Ingredient).WithMany().HasForeignKey(x => x.IngredientID);

            modelBuilder.Entity<SupportConversation>()
                .HasOne<User>().WithMany().HasForeignKey(x => x.UserID).OnDelete(restrict);
            modelBuilder.Entity<SupportMessage>()
                .HasOne(x => x.Conversation).WithMany(x => x.Messages)
                .HasForeignKey(x => x.ConversationID).OnDelete(restrict);
            modelBuilder.Entity<SupportMessage>()
                .HasOne<User>().WithMany().HasForeignKey(x => x.UserID).OnDelete(setNull);
            modelBuilder.Entity<SupportTicket>()
                .HasOne(x => x.Conversation).WithMany().HasForeignKey(x => x.ConversationID).OnDelete(restrict);
            modelBuilder.Entity<SupportTicket>()
                .HasOne<User>().WithMany().HasForeignKey(x => x.UserID).OnDelete(restrict);
            modelBuilder.Entity<SupportTicket>()
                .HasOne<User>().WithMany().HasForeignKey(x => x.OperatorID).OnDelete(setNull);

            modelBuilder.Entity<SupplyRequest>()
                .HasOne(x => x.Ingredient).WithMany().HasForeignKey(x => x.IngredientID).OnDelete(restrict);
            modelBuilder.Entity<SupplyRequest>()
                .HasOne(x => x.SupplierUser).WithMany().HasForeignKey(x => x.SupplierUserID).OnDelete(setNull);
            modelBuilder.Entity<Stocktake>()
                .HasOne<User>().WithMany().HasForeignKey(x => x.CreatedByUserID).OnDelete(restrict);
            modelBuilder.Entity<StocktakeItem>()
                .HasOne(x => x.Stocktake).WithMany(x => x.Items)
                .HasForeignKey(x => x.StocktakeID).OnDelete(restrict);
            modelBuilder.Entity<StocktakeItem>()
                .HasOne<Ingredient>().WithMany().HasForeignKey(x => x.IngredientID).OnDelete(restrict);
            modelBuilder.Entity<StockTransaction>()
                .HasOne(x => x.Ingredient).WithMany().HasForeignKey(x => x.IngredientID).OnDelete(restrict);
            modelBuilder.Entity<StockTransaction>()
                .HasOne<User>().WithMany().HasForeignKey(x => x.UserID).OnDelete(setNull);
            modelBuilder.Entity<PaymentTransaction>()
                .HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderID).OnDelete(restrict);
            modelBuilder.Entity<PaymentTransaction>()
                .HasOne<User>().WithMany().HasForeignKey(x => x.UserID).OnDelete(restrict);
            modelBuilder.Entity<KnowledgeArticle>()
                .HasOne<User>().WithMany().HasForeignKey(x => x.CreatedByUserID).OnDelete(setNull);

            modelBuilder.Entity<User>().HasQueryFilter(x => !IsBranchScopedRole || (CurrentBranchID.HasValue && x.BranchID == CurrentBranchID.Value));
            modelBuilder.Entity<RestaurantTable>().HasQueryFilter(x => !IsBranchScopedRole || (CurrentBranchID.HasValue && x.BranchID == CurrentBranchID.Value));
            modelBuilder.Entity<MenuCategory>().HasQueryFilter(x => !IsBranchScopedRole || (CurrentBranchID.HasValue && x.BranchID == CurrentBranchID.Value));
            modelBuilder.Entity<MenuItem>().HasQueryFilter(x => !IsBranchScopedRole || (CurrentBranchID.HasValue && x.BranchID == CurrentBranchID.Value));
            modelBuilder.Entity<Ingredient>().HasQueryFilter(x => !IsBranchScopedRole || (CurrentBranchID.HasValue && x.BranchID == CurrentBranchID.Value));
            modelBuilder.Entity<Order>().HasQueryFilter(x => !IsBranchScopedRole || (CurrentBranchID.HasValue && x.BranchID == CurrentBranchID.Value));
            modelBuilder.Entity<SupportConversation>().HasQueryFilter(x => !IsBranchScopedRole || (CurrentBranchID.HasValue && x.BranchID == CurrentBranchID.Value));
            modelBuilder.Entity<Stocktake>().HasQueryFilter(x => !IsBranchScopedRole || (CurrentBranchID.HasValue && x.BranchID == CurrentBranchID.Value));
            modelBuilder.Entity<OrderItem>().HasQueryFilter(x => !IsBranchScopedRole || (CurrentBranchID.HasValue && x.Order.BranchID == CurrentBranchID.Value));
            modelBuilder.Entity<Reservation>().HasQueryFilter(x => !IsBranchScopedRole || (CurrentBranchID.HasValue && x.Table.BranchID == CurrentBranchID.Value));
            modelBuilder.Entity<Delivery>().HasQueryFilter(x => !IsBranchScopedRole || (CurrentBranchID.HasValue && x.Order.BranchID == CurrentBranchID.Value));
            modelBuilder.Entity<TechCard>().HasQueryFilter(x => !IsBranchScopedRole || (CurrentBranchID.HasValue && x.MenuItem.BranchID == CurrentBranchID.Value));
            modelBuilder.Entity<SupplyRequest>().HasQueryFilter(x => !IsBranchScopedRole || (CurrentBranchID.HasValue && x.Ingredient.BranchID == CurrentBranchID.Value));
            modelBuilder.Entity<StocktakeItem>().HasQueryFilter(x => !IsBranchScopedRole || (CurrentBranchID.HasValue && x.Stocktake.BranchID == CurrentBranchID.Value));
            modelBuilder.Entity<StockTransaction>().HasQueryFilter(x => !IsBranchScopedRole || (CurrentBranchID.HasValue && x.Ingredient.BranchID == CurrentBranchID.Value));
            modelBuilder.Entity<PaymentTransaction>().HasQueryFilter(x => !IsBranchScopedRole || (CurrentBranchID.HasValue && x.Order.BranchID == CurrentBranchID.Value));
            modelBuilder.Entity<SupportMessage>().HasQueryFilter(x => !IsBranchScopedRole || (CurrentBranchID.HasValue && x.Conversation.BranchID == CurrentBranchID.Value));
            modelBuilder.Entity<SupportTicket>().HasQueryFilter(x => !IsBranchScopedRole || (CurrentBranchID.HasValue && x.Conversation.BranchID == CurrentBranchID.Value));
        }
    }
}
