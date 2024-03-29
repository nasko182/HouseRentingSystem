﻿namespace HouseRentingSystem.Data.Models;

using System.ComponentModel.DataAnnotations;

using static HouseRentingSystem.Common.EntityValidationConstants.House;

public class House
{
    public House()
    {
        this.Id = Guid.NewGuid();
        this.CreatedOn = DateTime.UtcNow;
        this.IsActive = true;
    }
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(TitleMaxLength)]
    public string Title { get; set; } = null!;

    [Required]
    [MaxLength(AddressMaxLength)]
    public string Address { get; set; } = null!;

    [Required]
    [MaxLength(DescriptionMaxLength)]
    public string Description { get; set; } = null!;

    [Required]
    [MaxLength(ImageUrlMaxLength)]
    public string ImageUrl { get; set; } = null!;

    public decimal PricePerMonth { get; set; }

    public DateTime CreatedOn { get; set; }

    public bool IsActive { get; set; }

    public int CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    public Guid AgentId { get; set; }

    public Agent Agent { get; set; } = null!;

    public Guid? RenterId { get; set; }

    public ApplicationUser? Renter { get; set; }


}
