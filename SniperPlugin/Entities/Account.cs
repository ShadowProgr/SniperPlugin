using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Eventing.Reader;
using System.Threading.Tasks;
using GoPlugin;

namespace SniperPlugin.Entities
{
    public class Account
    {
        public IManager Manager { get; set; }
        public List<Requirement> Requirements { get; set; }
        public ObservableCollection<Coord> SnipeQueue { get; set; }
        public Task<MethodResult> SnipeResult { get; set; }
    }
}
