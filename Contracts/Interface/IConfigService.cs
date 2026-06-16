using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Interface
{
    public interface IConfigService
    {
        AppConfigDto Get();
        void Update(AppConfigDto config);
    }
}
