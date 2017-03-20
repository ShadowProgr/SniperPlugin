using System.Collections.Generic;
using System.Threading.Tasks;
using GoPlugin;

namespace SniperPlugin.Entities
{
    public class Account
    {
        public IManager Manager { get; set; }
        public List<Requirement> Requirements { get; set; }
        public Task<MethodResult> SnipeResult { get; set; }
    }
}
