using POGOProtos.Enums;

namespace SniperPlugin.Entities
{
    public class Requirement
    {
        public PokemonId PokemonId { get; set; }
        public int CatchAmount { get; set; }
        public int AmountCaught { get; set; }
        public int MinIv { get; set; }
        public int MinCp { get; set; }
        public bool Snipe { get; set; }
    }
}
