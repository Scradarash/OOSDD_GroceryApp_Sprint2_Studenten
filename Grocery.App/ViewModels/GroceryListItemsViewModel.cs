using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grocery.App.Views;
using Grocery.Core.Interfaces.Services;
using Grocery.Core.Models;
using System.Collections.ObjectModel;

namespace Grocery.App.ViewModels
{
    [QueryProperty(nameof(GroceryList), nameof(GroceryList))]
    public partial class GroceryListItemsViewModel : BaseViewModel
    {
        private readonly IGroceryListItemsService _groceryListItemsService;
        private readonly IProductService _productService;
        public ObservableCollection<GroceryListItem> MyGroceryListItems { get; set; } = [];
        public ObservableCollection<Product> AvailableProducts { get; set; } = [];

        [ObservableProperty]
        GroceryList groceryList = new(0, "None", DateOnly.MinValue, "", 0);

        public GroceryListItemsViewModel(IGroceryListItemsService groceryListItemsService, IProductService productService)
        {
            _groceryListItemsService = groceryListItemsService;
            _productService = productService;
            Load(groceryList.Id);
        }

        private void Load(int id)
        {
            MyGroceryListItems.Clear();
            foreach (var item in _groceryListItemsService.GetAllOnGroceryListId(id)) MyGroceryListItems.Add(item);
            GetAvailableProducts();
        }

        private void GetAvailableProducts()
        {
            AvailableProducts.Clear();

            // alle producten ophalen
            var allProducts = _productService.GetAll();

            foreach (var product in allProducts)
            {
                bool onList = false;
                foreach (var item in MyGroceryListItems)
                {
                    if (item.ProductId == product.Id)
                    {
                        onList = true;
                        break;
                    }
                }
                if (product.Stock > 0 && !onList)
                {
                    AvailableProducts.Add(product);
                }
            }
        }

        partial void OnGroceryListChanged(GroceryList value)
        {
            Load(value.Id);
        }

        [RelayCommand]
        public async Task ChangeColor()
        {
            Dictionary<string, object> paramater = new() { { nameof(GroceryList), GroceryList } };
            await Shell.Current.GoToAsync($"{nameof(ChangeColorView)}?Name={GroceryList.Name}", true, paramater);
        }
        [RelayCommand]
        public void AddProduct(Product product)
        {
            if (product == null || product.Id <= 0) return;
            var newItem = new GroceryListItem(
                id: 0,
                groceryListId: GroceryList.Id,
                productId: product.Id,
                amount: 1
            );

            // Voeg toe via de service
            _groceryListItemsService.Add(newItem);

            // Voorraad aanpassen en opslaan
            product.Stock--;
            _productService.Update(product);

            // Lijst met beschikbare producten opnieuw gebruiken zodat t product er niet meer tussen staat
            GetAvailableProducts();

            // View updaten zodat de lijst en items synchroon blijven
            OnGroceryListChanged(GroceryList);
        }

    }
}
