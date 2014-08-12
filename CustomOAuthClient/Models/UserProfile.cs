namespace CustomOAuthClient.Models
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("UserProfile")]
    public class UserProfile
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public virtual int UserId { get; set; }

        public virtual string UserName { get; set; }

        public virtual ICollection<UserProfileExtraData> ExtraData { get; set; }
    }
}