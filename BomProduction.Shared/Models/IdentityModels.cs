using System;
using System.Collections.Generic;
using System.Text;

namespace BomProduction.Shared.Models
{
    public enum UserRole
    {
        Admin,
        Supervisor,
        Worker
    }

    public class RolePermissions
    {
        public bool CanViewBom { get; set; }
        public bool CanUpload { get; set; }
        public bool CanViewSchedule { get; set; }
        public bool CanManageUsers { get; set; }
        public bool CanEditDocuments { get; set; }
    }

    public class User
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public string? AvatarUrl { get; set; }
        public bool Active { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
