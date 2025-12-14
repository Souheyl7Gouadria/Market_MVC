using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Market.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        public string Name { get; set; }
        public string? StreetAddress { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        // it is possible that the user is a customer, so he does not belong to a company
        public int? CompanyId { get; set; }
        // navigation property
        [ForeignKey("CompanyId")]
        [ValidateNever]
        public Company? Company { get; set; }

        // additional property to store the role of the user, we need it for the Users table
        [NotMapped]
        public string Role { get; set; }
    }
}
