using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;

namespace RockPaperScissorAPI.Controllers
{


    [Route("api/[controller]")]
    [ApiController]
    public class RockPaperScissorController : ControllerBase
    {
        private IConfiguration _configuration;
        public RockPaperScissorController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost]
        [Route("Player")]
        public JsonResult Player([FromForm] string name)
        {
            // Primero verificamos si el jugador ya existe
            string checkQuery = "SELECT COUNT(1) FROM Players WHERE FullName = @name";
            string insertQuery = "INSERT INTO Players VALUES(@name)";

            DataTable table = new DataTable();
            string sqlDataSource = _configuration.GetConnectionString("RockPaperScissorsCon");
            SqlDataReader myReader;

            using (SqlConnection myCon = new SqlConnection(sqlDataSource))
            {
                myCon.Open();

                // Verificar si el jugador ya existe
                using (SqlCommand checkCommand = new SqlCommand(checkQuery, myCon))
                {
                    checkCommand.Parameters.AddWithValue("@name", name);
                    int count = (int)checkCommand.ExecuteScalar(); // Devuelve la cantidad de registros con ese nombre

                    if (count > 0)
                    {
                        // Si ya existe, retornamos "Bienvenido de nuevo"
                        myCon.Close();
                        return new JsonResult($"Bienvenido de nuevo {name}!");
                    }
                }

                // Si no existe, insertamos el nuevo jugador
                using (SqlCommand insertCommand = new SqlCommand(insertQuery, myCon))
                {
                    insertCommand.Parameters.AddWithValue("@name", name);
                    myReader = insertCommand.ExecuteReader();
                    table.Load(myReader);
                    myReader.Close();
                }

                myCon.Close();
                return new JsonResult($"New player {name}!");
            }
        }


    }
}
