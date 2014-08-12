namespace CustomOAuthClient.Models
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class RegisterExternalLoginModel
    {
        [Required]
        [Display(Name = "User name")]
        public string UserName { get; set; }

        public string ExternalLoginData { get; set; }

        public IDictionary<string, string> ExtraData { get; set; }
    }
}