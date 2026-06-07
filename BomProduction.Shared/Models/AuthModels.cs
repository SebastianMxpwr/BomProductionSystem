using System;
using System.Collections.Generic;
using System.Text;

namespace BomProduction.Shared.Models
{
    public record LoginRequest(string Email, string Password);
}
