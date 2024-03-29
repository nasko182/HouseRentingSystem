﻿namespace HouseRentingSystem.Web.Controllers;

using Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Data.Interfaces;
using Services.Data.Models.House;
using ViewModels.House;

using static Common.NotificationMessagesConstants;

public class HouseController : BaseController
{
    private readonly ICategoryService _categoryService;
    private readonly IAgentService _agentService;
    private readonly IHouseService _houseService;

    public HouseController(ICategoryService categoryService, IAgentService agentService, IHouseService houseService)
    {
        this._categoryService = categoryService;
        this._agentService = agentService;
        this._houseService = houseService;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> All([FromQuery] AllHousesQueryModel model)
    {
        AllHousesFilteredAndPagedServiceModel serviceModel = await this._houseService.AllAsync(model);

        model.Houses = serviceModel.Houses;
        model.TotalHouses = serviceModel.TotalHouseCount;
        model.Categories = await this._categoryService.GetAllCategoryNamesAsync();

        return this.View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Mine()
    {
        string userId = this.User.GetId()!;
        try
        {
            List<HouseAllViewModel> houses = new List<HouseAllViewModel>();
            if (this.User.IsAdmin())
            {
                string? agentId = await this._agentService.GetAgentIdByUserIdAsync(userId);

                houses.AddRange(await this._houseService.AllByAgentIdAsync(agentId!));
                houses.AddRange(await this._houseService.AllByUserIdAsync(userId));

                houses = houses.DistinctBy(h => h.Id).ToList();
            }
            else if (await this._agentService.AgentExistByUserIdAsync(userId))
            {
                string? agentId = await this._agentService.GetAgentIdByUserIdAsync(userId);

                houses.AddRange(await this._houseService.AllByAgentIdAsync(agentId!));

            }
            else
            {
                houses.AddRange(await this._houseService.AllByUserIdAsync(userId));

            }

            return this.View(houses);
        }
        catch
        {
            return this.GeneralError();
        }
    }

    [HttpGet]
    public async Task<IActionResult> Add()
    {
        bool isAgent = await this._agentService.AgentExistByUserIdAsync(this.User.GetId()!);
        if (!isAgent)
        {
            this.TempData[ErrorMessage] = "You must become an agent in order to add new houses!";

            return this.RedirectToAction("Become", "Agent");
        }

        try
        {
            HouseFormModel formModel = new HouseFormModel()
            {
                Categories = await this._categoryService.AllCategoriesAsync()
            };

            return this.View(formModel);
        }
        catch
        {
            return this.GeneralError();
        }
    }

    [HttpPost]
    public async Task<IActionResult> Add(HouseFormModel model)
    {
        bool isAgent = await this._agentService.AgentExistByUserIdAsync(this.User.GetId()!);
        if (!isAgent)
        {
            this.TempData[ErrorMessage] = "You must become an agent in order to add new houses!";

            return this.RedirectToAction("Become", "Agent");
        }

        bool isCategoryExist = await this._categoryService.ExistsByIdAsync(model.CategoryId);
        if (!isCategoryExist)
        {
            this.ModelState.AddModelError(nameof(model.CategoryId), "Selected category does not exist.");
        }

        if (!this.ModelState.IsValid)
        {
            model.Categories = await this._categoryService.AllCategoriesAsync();

            return this.View(model);
        }

        string houseId;
        try
        {
            string? agentId = await this._agentService
                .GetAgentIdByUserIdAsync(this.User.GetId()!);

            houseId = await this._houseService.CreateHouseAsync(model, agentId!);
        }
        catch
        {
            this.ModelState.AddModelError(String.Empty,
                "Unexpected error occurred while trying to add your new house! Please try again later or contact administrator.");

            model.Categories = await this._categoryService.AllCategoriesAsync();
            return this.View(model);
        }

        this.TempData[SuccessMessage] = "House was added successfully!";
        return this.RedirectToAction("Details", "House", new { id = houseId });
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Details(string id)
    {

        bool houseExist = await this._houseService.ExistByIdAsync(id);
        if (!houseExist)
        {
            this.TempData[ErrorMessage] = "House with provided id does not exist!";

            return this.RedirectToAction("All", "House");
        }

        try
        {
            HouseDetailsViewModel model = await this._houseService.GetDetailsByHouseIdAsync(id);

            return this.View(model);
        }
        catch
        {
            return this.GeneralError();
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        IActionResult? validationRedirectAction = await this.MakeValidations(id);
        if (validationRedirectAction != null)
        {
            return validationRedirectAction;
        }

        try
        {
            HouseFormModel model = await this._houseService.GetHouseForEditByIdAsync(id);

            model.Categories = await this._categoryService.AllCategoriesAsync();

            return this.View(model);
        }
        catch
        {
            return this.GeneralError();
        }

    }

    [HttpPost]
    public async Task<IActionResult> Edit(string id, HouseFormModel model)
    {
        if (!this.ModelState.IsValid)
        {
            model.Categories = await this._categoryService.AllCategoriesAsync();
            return this.View(model);
        }

        IActionResult? validationRedirectAction = await this.MakeValidations(id);
        if (validationRedirectAction != null)
        {
            return validationRedirectAction;
        }

        try
        {
            await this._houseService.EditHouseByIdAndFormModelAsync(id, model);
        }
        catch
        {
            this.ModelState.AddModelError(string.Empty,
                "Unexpected error occurred while trying to update the house. Please try again later or contact administrator.");

            model.Categories = await this._categoryService.AllCategoriesAsync();
            return this.View(model);
        }

        this.TempData[SuccessMessage] = "House was edited successfully!";
        return this.RedirectToAction("Details", "House", new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Delete(string id)
    {
        IActionResult? validationRedirectAction = await this.MakeValidations(id);
        if (validationRedirectAction != null)
        {
            return validationRedirectAction;
        }

        try
        {
            DeleteHouseFormModel model = await this._houseService.GetHouseForDeleteAsync(id);

            return this.View(model);

        }
        catch
        {
            return this.GeneralError();
        }
    }

    [HttpPost]
    public async Task<IActionResult> Delete(string id, DeleteHouseFormModel model)
    {
        IActionResult? validationRedirectAction = await this.MakeValidations(id);
        if (validationRedirectAction != null)
        {
            return validationRedirectAction;
        }

        try
        {
            await this._houseService.DeleteByIdAsync(id);

            this.TempData[SuccessMessage] = "The house was successfully deleted";

            return this.RedirectToAction("Mine", "House");
        }
        catch
        {
            return this.GeneralError();
        }
    }

    [HttpPost]
    public async Task<IActionResult> Rent(string id)
    {
        bool houseExist = await this._houseService.ExistByIdAsync(id);
        if (!houseExist)
        {
            this.TempData[ErrorMessage] = "House with provided id does not exist!";

            return this.RedirectToAction("All", "House");
        }

        string userId = this.User.GetId()!;
        bool isAgent = await this._agentService.AgentExistByUserIdAsync(userId);
        if (isAgent && !this.User.IsAdmin())
        {
            this.TempData[ErrorMessage] = "Agents cannot rent houses";

            return this.RedirectToAction("Index", "Home");

        }

        bool isRented = await this._houseService.IsRentedAsync(id);
        if (isRented)
        {
            this.TempData[ErrorMessage] = "Selected house is already rented.Please select another house";
            return this.RedirectToAction("All", "House");
        }

        try
        {
            await this._houseService.RentAsync(id, userId);

            return this.RedirectToAction("Mine", "House");
        }
        catch
        {
            return this.GeneralError();
        }
    }

    [HttpPost]
    public async Task<IActionResult> Leave(string id)
    {
        bool houseExist = await this._houseService.ExistByIdAsync(id);
        if (!houseExist)
        {
            this.TempData[ErrorMessage] = "House with provided id does not exist!";

            return this.RedirectToAction("All", "House");
        }

        string? userId = this.User.GetId();
        bool isAgent = await this._agentService.AgentExistByUserIdAsync(userId!);
        if (isAgent && !this.User.IsAdmin())
        {
            this.TempData[ErrorMessage] = "Agents cannot leave houses";
            return this.RedirectToAction("Index", "Home");

        }

        bool isRented = await this._houseService.IsRentedAsync(id);
        if (!isRented)
        {
            this.TempData[ErrorMessage] = "This house is not rented yet. You can't leave not rented house";
            return this.RedirectToAction("Mine", "House");
        }

        bool isRentedByUser = await this._houseService.IsRentedByUserAsync(id, userId!);
        if (!isRentedByUser && !this.User.IsAdmin())
        {
            this.TempData[ErrorMessage] = "Selected house is not rented by you. Please select one of your houses";
            return this.RedirectToAction("Mine", "House");
        }

        try
        {
            if (this.User.IsAdmin())
            {
                userId = await this._houseService.GetUserIdByHouseId(id);
            }

            await this._houseService.LeaveAsync(id, userId!);

            return this.RedirectToAction("Mine", "House");
        }
        catch
        {
            return this.GeneralError();
        }
    }

    private async Task<IActionResult?> MakeValidations(string id)
    {
        bool houseExist = await this._houseService.ExistByIdAsync(id);
        if (!houseExist)
        {
            this.TempData[ErrorMessage] = "House with provided id does not exist!";

            return this.RedirectToAction("All", "House");
        }

        bool isAgent = await this._agentService.AgentExistByUserIdAsync(this.User.GetId()!);
        if (!isAgent)
        {
            this.TempData[ErrorMessage] = "You must become an agent in order to edit houses!";

            return this.RedirectToAction("Become", "Agent");

        }

        string? agentId = await this._agentService.GetAgentIdByUserIdAsync(this.User.GetId()!);

        bool isOwner = await this._houseService.IsAgentWithIdOwnerOfHouseWithIdAsync(id, agentId!);

        if (!isOwner && !this.User.IsAdmin())
        {
            this.TempData[ErrorMessage] = "You must be the agent owner of the house!";

            return this.RedirectToAction("Mine", "House");
        }

        return null;
    }

    private IActionResult GeneralError()
    {
        this.TempData[ErrorMessage] = "Unexpected error occurred. Please try again later or contact administrator.";

        return this.RedirectToAction("Index", "Home");
    }
}
