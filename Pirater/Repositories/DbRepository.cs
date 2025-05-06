using Microsoft.Extensions.Configuration;
using Npgsql;
using Pirater.Tabeller;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pirater.Repositories
{
    public class DbRepository
    {
        private readonly string _connectionString;
        public DbRepository()
        {
            var config = new ConfigurationBuilder()
                             .AddUserSecrets<DbRepository>()
                             .Build();
            _connectionString = config.GetConnectionString("DefaultConnection");
        }

        public async Task RegNewPirate(Pirate pirate)
        {
            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                using var command = new NpgsqlCommand("INSERT INTO pirate(name) " +
                                                        "VALUES(@pirate_name)", conn);

                // Satt upp kommando lagg in parametrar. pirate.Name ersätter pirate_name
                command.Parameters.AddWithValue("pirate_name", pirate.Name);

                // kör
                var result = await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                throw;
            }

        }
    }
}
