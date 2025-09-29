using System.ComponentModel.DataAnnotations;

namespace UserManagmentSystem.Web.Models;

public class ResetPasswordViewModel
{
    [Required(ErrorMessage = "New password is required")]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    public string NewPassword { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Confirm New Password")]
    [Compare("NewPassword", ErrorMessage = "The password and confirmation password do not match")]
    public string ConfirmPassword { get; set; }

    [Required]
    public string Token { get; set; }

    public Guid UserId { get; set; }
}
