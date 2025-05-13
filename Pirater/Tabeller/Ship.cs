using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pirater.Tabeller
{
    public class Ship
    {
        public int Id {  get; set; }
        public string Name { get; set; }
        public int ShipTypeId { get; set; }

        public bool IsSunk { get; set; } //Tillagd för att kunna visa om skeppet är sänkt eller inte
    }
}
