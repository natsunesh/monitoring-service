using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts
{
    public interface IPipeClient
    {
        Task<PipeResponse> SendAsync(PipeRequest request, int timeoutMs = 10000);
    }
}
