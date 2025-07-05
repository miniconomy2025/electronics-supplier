using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace esAPI.Models
{
    [Table("companies")]
    public class Company
    {
        [Key]
        [Column("company_id")]
        public int CompanyId { get; set; }
        [Column("company_name")]
        public string CompanyName { get; set; } = string.Empty;
        [Column("bank_account_number")]
        public string? BankAccountNumber { get; set; }
    }
} 