﻿namespace HouseRentingSystem.Services.Data.Interfaces;

using Web.ViewModels.Category;

public interface ICategoryService
{
    Task<IEnumerable<HouseSelectCategoryFormModel>> AllCategoriesAsync();
    Task<bool> ExistsByIdAsync(int categoryId);

    Task<IEnumerable<string>> GetAllCategoryNamesAsync();
}
