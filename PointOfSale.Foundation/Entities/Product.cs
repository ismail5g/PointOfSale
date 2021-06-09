using System;
using System.ComponentModel;
using PointOfSale.DataAccessLayer;

namespace PointOfSale.Foundation
{
    public class Product : IEntity<Guid>
    {
        public Guid Id { get; set; }
        [DisplayName("Price")]
        public double Price { get; set; }
        public int Quantity { get; set; }

        public Guid CategoryId { get; set; }
        public Category Category { get; set; }
        public virtual SaleDetail SaleDetail { get; set; }
    }
}