using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;

namespace SistemaReservaciones
{
    public class Database
    {
        private static string connectionString = "Server=localhost;Database=reservaciones;Uid=root;Pwd=;";

        // Obtener eventos de la base de datos
        public static List<Evento> ObtenerEventos()
        {
            var eventos = new List<Evento>();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                string query = "SELECT * FROM eventos";
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            eventos.Add(new Evento
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Nombre = reader["Nombre"].ToString(),
                                Fecha = Convert.ToDateTime(reader["Fecha"]),
                                TotalAsientos = Convert.ToInt32(reader["TotalAsientos"]),
                                AsientosDisponibles = Convert.ToInt32(reader["AsientosDisponibles"])
                            });
                        }
                    }
                }
            }

            return eventos;
        }

        // Consultar disponibilidad de asientos de un evento específico
        public static int ConsultarDisponibilidad(int eventoId)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                string query = "SELECT AsientosDisponibles FROM eventos WHERE Id = @EventoId";
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@EventoId", eventoId);
                    object result = command.ExecuteScalar();
                    return Convert.ToInt32(result);
                }
            }
        }

        // Agregar una nueva reservación
        public static void AgregarReservacion(string cliente, int asientos, string fechaEvento, int eventoId)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Verificar disponibilidad de asientos
                        int asientosDisponibles = ConsultarDisponibilidad(eventoId);
                        if (asientos > asientosDisponibles)
                        {
                            throw new Exception("No hay suficientes asientos disponibles.");
                        }

                        // Insertar reservación
                        string query = "INSERT INTO reservaciones (EventoId, Cliente, AsientosReservados, FechaReserva) " +
                                       "VALUES (@EventoId, @Cliente, @Asientos, @FechaReserva)";
                        using (MySqlCommand command = new MySqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@EventoId", eventoId);
                            command.Parameters.AddWithValue("@Cliente", cliente);
                            command.Parameters.AddWithValue("@Asientos", asientos);
                            command.Parameters.AddWithValue("@FechaReserva", DateTime.Now);
                            command.ExecuteNonQuery();
                        }

                        // Actualizar asientos disponibles
                        query = "UPDATE eventos SET AsientosDisponibles = AsientosDisponibles - @Asientos WHERE Id = @EventoId";
                        using (MySqlCommand command = new MySqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@Asientos", asientos);
                            command.Parameters.AddWithValue("@EventoId", eventoId);
                            command.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw new Exception("Error al agregar la reservación: " + ex.Message);
                    }
                }
            }
        }

        // Obtener todas las reservaciones de la base de datos
        public static List<Reservacion> ObtenerReservaciones()
        {
            var reservaciones = new List<Reservacion>();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                string query = "SELECT * FROM reservaciones";
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            reservaciones.Add(new Reservacion
                            {
                                Cliente = reader["Cliente"].ToString(),
                                Asientos = Convert.ToInt32(reader["AsientosReservados"]),
                                FechaEvento = Convert.ToDateTime(reader["FechaReserva"])
                            });
                        }
                    }
                }
            }

            return reservaciones;
        }

        // Función para registrar el pago (esto es un ejemplo para integrar pagos en el sistema)
        public static void RegistrarPago(int reservacionId, decimal monto)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                string query = "INSERT INTO pagos (ReservacionId, Monto, FechaPago) VALUES (@ReservacionId, @Monto, @FechaPago)";
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ReservacionId", reservacionId);
                    command.Parameters.AddWithValue("@Monto", monto);
                    command.Parameters.AddWithValue("@FechaPago", DateTime.Now);
                    command.ExecuteNonQuery();
                }
            }
        }
    }

    // Clase para representar los eventos
    public class Evento
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public DateTime Fecha { get; set; }
        public int TotalAsientos { get; set; }
        public int AsientosDisponibles { get; set; }
    }

    // Clase para representar las reservaciones
    public class Reservacion
    {
        public string Cliente { get; set; }
        public int Asientos { get; set; }
        public DateTime FechaEvento { get; set; }
    }
}
