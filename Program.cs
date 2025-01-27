using System;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

class Program
{
    private const string ConnectionString ="Server=localhost;Database=reservaciones;Uid=root;Pwd=;"; // Reemplaza con tu cadena de conexión a la base de datos

    static async Task Main(string[] args)
    {
        Console.WriteLine("Bienvenido al sistema de reservaciones de eventos.\n");

        while (true)
        {
            Console.WriteLine("Por favor, selecciona una opción:");
            Console.WriteLine("1. Registrar una nueva reservación");
            Console.WriteLine("2. Ver todas las reservaciones");
            Console.WriteLine("3. Consultar disponibilidad de asientos");
            Console.WriteLine("4. Salir");

            string opcion = Console.ReadLine();

            switch (opcion)
            {
                case "1":
                    await RegistrarReservacionAsync();
                    break;
                case "2":
                    VerReservaciones();
                    break;
                case "3":
                    await ConsultarDisponibilidad();
                    break;
                case "4":
                    Console.WriteLine("Gracias por usar el sistema. ¡Adiós!");
                    return;
                default:
                    Console.WriteLine("Opción no válida. Por favor, intenta nuevamente.");
                    break;
            }
        }
    }

    private static async Task RegistrarReservacionAsync()
    {
        Console.WriteLine("\nIngresa el nombre del cliente:");
        string cliente = Console.ReadLine();

        Console.WriteLine("Ingresa el número de asientos a reservar:");
        int asientos = int.Parse(Console.ReadLine());

        Console.WriteLine("Ingresa el ID del evento:");
        int eventoId = int.Parse(Console.ReadLine());

        try
        {
            // Consultar disponibilidad de asientos en la base de datos
            int asientosDisponibles = await ConsultarDisponibilidadAsientos(eventoId);

            if (asientosDisponibles >= asientos)
            {
                // Realizar reservación
                using (SqlConnection connection = new SqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();

                    // Insertar reservación en la tabla reservaciones
                    string insertReservacionQuery = @"INSERT INTO reservaciones (EventoId, Cliente, AsientosReservados, FechaReserva) 
                                                     VALUES (@EventoId, @Cliente, @AsientosReservados, @FechaReserva);
                                                     SELECT SCOPE_IDENTITY();"; // Obtener el ID de la nueva reservación

                    DateTime fechaReserva = DateTime.Now;

                    using (SqlCommand command = new SqlCommand(insertReservacionQuery, connection))
                    {
                        command.Parameters.AddWithValue("@EventoId", eventoId);
                        command.Parameters.AddWithValue("@Cliente", cliente);
                        command.Parameters.AddWithValue("@AsientosReservados", asientos);
                        command.Parameters.AddWithValue("@FechaReserva", fechaReserva);

                        // Ejecutar el comando y obtener el ID de la nueva reservación
                        int nuevaReservacionId = Convert.ToInt32(await command.ExecuteScalarAsync());

                        Console.WriteLine($"\nReservación completada para el cliente '{cliente}'. Asientos reservados: {asientos}");

                        // Actualizar la tabla eventos: restar los asientos reservados de AsientosDisponibles
                        string updateEventosQuery = @"UPDATE eventos SET AsientosDisponibles = AsientosDisponibles - @AsientosReservados
                                                     WHERE Id = @EventoId;";

                        using (SqlCommand updateCommand = new SqlCommand(updateEventosQuery, connection))
                        {
                            updateCommand.Parameters.AddWithValue("@AsientosReservados", asientos);
                            updateCommand.Parameters.AddWithValue("@EventoId", eventoId);

                            int rowsAffected = await updateCommand.ExecuteNonQueryAsync();

                            Console.WriteLine($"Asientos disponibles actualizados en la base de datos. Asientos restantes: {asientosDisponibles - asientos}");
                        }

                        // Generar y guardar el PDF localmente
                        string pdfPath = $"Boleto_Reservacion_{nuevaReservacionId}.pdf";
                        GenerarPDF(cliente, eventoId, asientos, fechaReserva, pdfPath);
                        Console.WriteLine($"Boleto generado y guardado localmente en: {pdfPath}");
                    }
                }
            }
            else
            {
                Console.WriteLine($"Lo sentimos, no hay suficientes asientos disponibles. Asientos disponibles: {asientosDisponibles}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al registrar la reservación: {ex.Message}");
        }
    }

    private static void GenerarPDF(string cliente, int eventoId, int asientos, DateTime fechaReserva, string filePath)
    {
        // Aquí deberías implementar la lógica para generar el PDF con la información de la reservación
        // En este ejemplo, simplemente se guarda un archivo vacío como simulación
        File.WriteAllText(filePath, $"Reservación para cliente: {cliente}, Evento ID: {eventoId}, Asientos: {asientos}, Fecha de reserva: {fechaReserva}");
    }

    private static void VerReservaciones()
    {
        try
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                string query = @"SELECT Id, EventoId, Cliente, AsientosReservados, FechaReserva FROM reservaciones;";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        Console.WriteLine("\nListado de reservaciones:");
                        while (reader.Read())
                        {
                            int id = reader.GetInt32(0);
                            int eventoId = reader.GetInt32(1);
                            string cliente = reader.GetString(2);
                            int asientosReservados = reader.GetInt32(3);
                            DateTime fechaReserva = reader.GetDateTime(4);

                            Console.WriteLine($"ID: {id}, Evento ID: {eventoId}, Cliente: {cliente}, Asientos: {asientosReservados}, Fecha de reserva: {fechaReserva}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al consultar las reservaciones: {ex.Message}");
        }
    }

    private static async Task ConsultarDisponibilidad()
    {
        Console.WriteLine("\nIngresa el ID del evento para consultar la disponibilidad de asientos:");
        int eventoId = int.Parse(Console.ReadLine());

        try
        {
            int asientosDisponibles = await ConsultarDisponibilidadAsientos(eventoId);

            Console.WriteLine($"Asientos disponibles para el evento ID {eventoId}: {asientosDisponibles}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al consultar la disponibilidad de asientos: {ex.Message}");
        }
    }

    private static async Task<int> ConsultarDisponibilidadAsientos(int eventoId)
    {
        // Consultar la tabla eventos para obtener AsientosDisponibles
        using (SqlConnection connection = new SqlConnection(ConnectionString))
        {
            await connection.OpenAsync();

            string query = "SELECT AsientosDisponibles FROM eventos WHERE Id = @EventoId;";

            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@EventoId", eventoId);

                object result = await command.ExecuteScalarAsync();

                if (result != null && result != DBNull.Value)
                {
                    return (int)result;
                }
                else
                {
                    throw new Exception($"No se encontró el evento con ID {eventoId}.");
                }
            }
        }
    }
}		