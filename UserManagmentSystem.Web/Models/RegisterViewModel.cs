using System.ComponentModel.DataAnnotations;

namespace UserManagmentSystem.Web.Models;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Name is required!")]
    [Display(Name = "Name")]
    public string Name { get; set; }

    [Display(Name = "Position")]
    public string Position { get; set; }

    [Required(ErrorMessage = "Email is required!")]
    [EmailAddress(ErrorMessage = "Invalid email format!")]
    [Display(Name = "elektron manzil")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Password is required!")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Confirm the password")]
    [Compare("Password", ErrorMessage = "Passwords are not same")]
    public string ConfirmPassword { get; set; }
}
