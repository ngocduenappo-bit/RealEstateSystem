using System.Collections.Generic;
using System.Data.Entity;

namespace RealEstateSystem.Models
{
    public class RealEstateDbContext : DbContext
    {
        // "RealEstateDbContext" phai trung ten voi connection string trong Web.config
        public RealEstateDbContext() : base("name=RealEstateDbContext")
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<PropertyType> PropertyTypes { get; set; }
        public DbSet<TransactionType> TransactionTypes { get; set; }
        public DbSet<Province> Provinces { get; set; }
        public DbSet<District> Districts { get; set; }
        public DbSet<Amenity> Amenities { get; set; }
        public DbSet<LegalStatus> LegalStatuses { get; set; }
        public DbSet<FurnitureStatus> FurnitureStatuses { get; set; }
        public DbSet<Property> Properties { get; set; }
        public DbSet<PropertyImage> PropertyImages { get; set; }
        public DbSet<PropertyAmenity> PropertyAmenities { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<ZaloContact> ZaloContacts { get; set; }
        public DbSet<SavedListing> SavedListings { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Database da duoc tao san bang script SQL, khong de EF tu dong
            // so sanh/sua cau truc bang (xem them Database.SetInitializer trong Global.asax)

            // ---- District -> Province ----
            modelBuilder.Entity<District>()
                .HasRequired(d => d.Province)
                .WithMany(p => p.Districts)
                .HasForeignKey(d => d.ProvinceId)
                .WillCascadeOnDelete(false); // chan xoa tinh/thanh neu con quan/huyen

            // ---- Property -> cac bang danh muc va chu so huu ----
            // Tat ca deu WillCascadeOnDelete(false): khong cho xoa danh muc
            // dang co tin dang su dung, khop voi rang buoc da thiet ke trong SQL
            modelBuilder.Entity<Property>()
                .HasRequired(p => p.Owner)
                .WithMany(u => u.OwnedProperties)
                .HasForeignKey(p => p.OwnerUserId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Property>()
                .HasRequired(p => p.PropertyType)
                .WithMany(t => t.Properties)
                .HasForeignKey(p => p.PropertyTypeId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Property>()
                .HasRequired(p => p.TransactionType)
                .WithMany(t => t.Properties)
                .HasForeignKey(p => p.TransactionTypeId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Property>()
                .HasRequired(p => p.District)
                .WithMany(d => d.Properties)
                .HasForeignKey(p => p.DistrictId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Property>()
                .HasRequired(p => p.LegalStatus)
                .WithMany(l => l.Properties)
                .HasForeignKey(p => p.LegalStatusId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Property>()
                .HasRequired(p => p.FurnitureStatus)
                .WithMany(f => f.Properties)
                .HasForeignKey(p => p.FurnitureStatusId)
                .WillCascadeOnDelete(false);

            // ---- PropertyImage -> Property: xoa tin dang thi xoa luon anh ----
            modelBuilder.Entity<PropertyImage>()
                .HasRequired(pi => pi.Property)
                .WithMany(p => p.Images)
                .HasForeignKey(pi => pi.PropertyId)
                .WillCascadeOnDelete(true);

            // ---- PropertyAmenity (bang trung gian N-N) ----
            modelBuilder.Entity<PropertyAmenity>()
                .HasKey(pa => new { pa.PropertyId, pa.AmenityId });

            modelBuilder.Entity<PropertyAmenity>()
                .HasRequired(pa => pa.Property)
                .WithMany(p => p.PropertyAmenities)
                .HasForeignKey(pa => pa.PropertyId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<PropertyAmenity>()
                .HasRequired(pa => pa.Amenity)
                .WithMany(a => a.PropertyAmenities)
                .HasForeignKey(pa => pa.AmenityId)
                .WillCascadeOnDelete(false);

            // ---- Conversation -> Property (optional, co the NULL) ----
            modelBuilder.Entity<Conversation>()
                .HasOptional(c => c.Property)
                .WithMany(p => p.Conversations)
                .HasForeignKey(c => c.PropertyId)
                .WillCascadeOnDelete(false);

            // ---- Conversation -> User (2 quan he khac nhau: customer va agent) ----
            modelBuilder.Entity<Conversation>()
                .HasRequired(c => c.Customer)
                .WithMany(u => u.ConversationsAsCustomer)
                .HasForeignKey(c => c.CustomerUserId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Conversation>()
                .HasRequired(c => c.Agent)
                .WithMany(u => u.ConversationsAsAgent)
                .HasForeignKey(c => c.AgentUserId)
                .WillCascadeOnDelete(false);

            // ---- Message -> Conversation: xoa cuoc tro chuyen thi xoa luon tin nhan ----
            modelBuilder.Entity<Message>()
                .HasRequired(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<Message>()
                .HasRequired(m => m.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(m => m.SenderUserId)
                .WillCascadeOnDelete(false);

            // ---- ZaloContact -> Property: xoa tin dang thi xoa luon log ----
            modelBuilder.Entity<ZaloContact>()
                .HasRequired(z => z.Property)
                .WithMany(p => p.ZaloContacts)
                .HasForeignKey(z => z.PropertyId)
                .WillCascadeOnDelete(true);

            // ---- SavedListing -> User / Property  ----
            modelBuilder.Entity<SavedListing>()
                .HasRequired(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<SavedListing>()
                .HasRequired(s => s.Property)
                .WithMany()
                .HasForeignKey(s => s.PropertyId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Property>()
                .HasOptional(p => p.CreatedBy)
                .WithMany()
                .HasForeignKey(p => p.CreatedByUserId)
                .WillCascadeOnDelete(false);

            base.OnModelCreating(modelBuilder);
        }
    }
}