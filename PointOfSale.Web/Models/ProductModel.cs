using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Autofac;
using Microsoft.AspNetCore.Mvc.Rendering;
using PointOfSale.Foundation;
using PointOfSale.Foundation.Services;

namespace PointOfSale.Web.Models
{
    public class ProductModel
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly IMonthDetailService _monthDetailService;

        public ProductModel(IProductService productService, ICategoryService categoryService, IMonthDetailService monthDetailService)
        {
            _productService = productService;
            _categoryService = categoryService;
            _monthDetailService = monthDetailService;
        }

        public ProductModel()
        {
            _productService = Startup.AutofacContainer.Resolve<IProductService>();
            _categoryService = Startup.AutofacContainer.Resolve<ICategoryService>();
            _monthDetailService = Startup.AutofacContainer.Resolve<IMonthDetailService>();
            CategoryList = BuildCategoryList();
        }

        public Guid Id { get; set; }
        [Required(ErrorMessage = "Price Requred")]
        public double Price { get; set; }
        [Required(ErrorMessage = "Quantity Requred")]
        public int Quantity { get; set; }
        [Required(ErrorMessage = "Category Requred")]
        public Guid CategoryId { get; set; }
        public SelectList CategoryList { get; set; }
        [Required(ErrorMessage = "Name Requred")]
        public string Name { get; set; }

        internal object GetAllProducts(DataTablesAjaxRequestModel tableModel)
        {
            var (total, totalDisplay, records) = _productService.GetProductList(tableModel.PageIndex, tableModel.PageSize, tableModel.SearchText,
            tableModel.GetSortText(
                new[]{
                    "Name",
                    "Price",
                    "Quantity",
                }
            ));
            return new
            {
                recordsTotal = total,
                recordsFiltered = totalDisplay,
                data = (from record in records
                        select new object[]
                        {
                            record.Name,
                            record.Price,
                            record.Quantity,
                            record.Category.Name,
                            record.SaleDetails == null ? record.Quantity : record.SaleDetails.Where(x => x.ProductId == record.Id).Select(x => x.Quantity),
                            record.Id.ToString(),
                        }
                    ).ToArray()
            };
        }

        internal void SaveProduct(ProductModel model)
        {
            var product = new Product()
            {
                Name = model.Name,
                Price = model.Price,
                Quantity = model.Quantity,
                CategoryId = model.CategoryId
            };

            _productService.AddProduct(product);

            var category = _categoryService.GetCategory(model.CategoryId);
            category.Invest += ((product.Quantity) * (product.Price));
            category.NoOfProduct += product.Quantity;
            category.StockProduct += product.Quantity;

            _categoryService.UpdateCategory(category);


            var monthDetails = _monthDetailService.MonthDetails().FirstOrDefault(x => x.CategoryId == category.Id);
            monthDetails.Invest += category.Invest;
            monthDetails.Loss += ((product.Quantity) * (product.Price));

            _monthDetailService.UpdateMonthDetail(monthDetails);
        }

        internal void UpdateProduct(Guid id, ProductModel model)
        {
            var product = _productService.GetProduct(id);
            product.Name = model.Name;
            product.Price = model.Price;
            product.Quantity = model.Quantity;
            product.CategoryId = model.CategoryId;

            _productService.UpdateProduct(product);

            var category = _categoryService.GetCategory(product.CategoryId);
            category.Invest += ((product.Quantity) * (product.Price));
            category.NoOfProduct += product.Quantity;
            category.StockProduct += product.Quantity;

            _categoryService.UpdateCategory(category);

            var monthDetails = _monthDetailService.MonthDetails().FirstOrDefault(x => x.CategoryId == category.Id);
            monthDetails.Invest += category.Invest;
            monthDetails.Loss += ((product.Quantity) * (product.Price));

            _monthDetailService.UpdateMonthDetail(monthDetails);
        }

        internal ProductModel BuildEditProductModel(Guid id)
        {
            var product = _productService.GetProduct(id);
            return new ProductModel
            {
                Name = product.Name,
                Price = product.Price,
                Quantity = product.Quantity,
                CategoryList = BuildCategoryList(product.CategoryId),
                CategoryId = product.CategoryId,
                Id = product.Id
            };
        }

        private SelectList BuildCategoryList(object selected = null)
        {
            var categories = _categoryService.Categories();                        
            return new SelectList(categories, "Id", "Name", selected);
        }

        internal bool DeleteProduct(Guid id)
        {
            var product = _productService.GetProduct(id);
            if (product == null) return false;
            var category = _categoryService.GetCategory(product.CategoryId);
            category.Invest -= ((product.Quantity) * (product.Price));
            category.NoOfProduct -= product.Quantity;
            category.StockProduct -= product.Quantity;

            var monthDetail = _monthDetailService.MonthDetails().FirstOrDefault(x => x.CategoryId == category.Id);
            monthDetail.Invest += category.Invest;
            monthDetail.Loss += ((product.Quantity) * (product.Price));

            return _productService.DeleteProduct(id);
        }
    }
}