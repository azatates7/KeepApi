using System;
using System.Collections.Generic;
using System.Text;

namespace KeepApi.Application.Interfaces
{
    public interface ICurrentUserService
    {
        Guid UserId { get; }
    }
}
