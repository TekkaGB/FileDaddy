using System.Collections.ObjectModel;

namespace FNF_Mod_Manager
{
    public class Mod
    {
        public string name { get; set; }
        public bool enabled { get; set; }
    }
    public class Config
    {
        public string exe { get; set; }
        public ObservableCollection<Mod> ModList { get; set; }
    }
}
