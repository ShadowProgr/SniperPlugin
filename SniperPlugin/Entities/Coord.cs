using POGOProtos.Enums;

namespace SniperPlugin.Entities
{
    public class Coord
    {
        public PokemonId Pokemon { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }
        public int Iv { get; set; }
        //public int Cp { get; set; }

        public Coord()
        {
            Pokemon = PokemonId.Missingno;
            Lat = 0.0;
            Lon = 0.0;
            Iv = 0;
            //Cp = 0;
        }

        public bool Valid()
        {
            return !Pokemon.Equals(PokemonId.Missingno);
        }
    }
}
