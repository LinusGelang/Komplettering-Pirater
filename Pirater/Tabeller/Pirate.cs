using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Pirater.Tabeller
{
    public class Pirate
    {
        public int Id {  get; set; }

        public string Name { get; set; }

        public int RankId { get; set; }

        public int ShipId { get; set; }

        public List<Parrot> Parrots { get; set; }

        public string GetName =>  $"{Name}  {Parrots?.First()?.Name}";

    }

}
