using System;
using System.IO;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

class Program
{
    private const string ConnectionString = "Server=localhost;Database=reservaciones;Uid=root;Pwd=;"; // Ajusta según tu configuración

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
            int asientosDisponibles = await ConsultarDisponibilidadAsientos(eventoId);

            if (asientosDisponibles >= asientos)
            {
                using (MySqlConnection connection = new MySqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();

                    string insertReservacionQuery = @"INSERT INTO reservaciones (EventoId, Cliente, AsientosReservados, FechaReserva) 
                                                      VALUES (@EventoId, @Cliente, @AsientosReservados, @FechaReserva);";

                    DateTime fechaReserva = DateTime.Now;

                    using (MySqlCommand command = new MySqlCommand(insertReservacionQuery, connection))
                    {
                        command.Parameters.AddWithValue("@EventoId", eventoId);
                        command.Parameters.AddWithValue("@Cliente", cliente);
                        command.Parameters.AddWithValue("@AsientosReservados", asientos);
                        command.Parameters.AddWithValue("@FechaReserva", fechaReserva);

                        await command.ExecuteNonQueryAsync();

                        Console.WriteLine($"\nReservación completada para el cliente '{cliente}'. Asientos reservados: {asientos}");
                    }

                    string updateEventosQuery = @"UPDATE eventos SET AsientosDisponibles = AsientosDisponibles - @AsientosReservados
                                                  WHERE Id = @EventoId;";

                    using (MySqlCommand updateCommand = new MySqlCommand(updateEventosQuery, connection))
                    {
                        updateCommand.Parameters.AddWithValue("@AsientosReservados", asientos);
                        updateCommand.Parameters.AddWithValue("@EventoId", eventoId);

                        await updateCommand.ExecuteNonQueryAsync();
                        Console.WriteLine("Disponibilidad de asientos actualizada.");
                    }
                }

                // Generar PDF
                string filePath = GenerarBoletoPDF(cliente, eventoId, asientos);
                Console.WriteLine($"\nBoleto generado: {filePath}");
            }
            else
            {
                Console.WriteLine($"No hay suficientes asientos disponibles. Asientos restantes: {asientosDisponibles}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al registrar la reservación: {ex.Message}");
        }
    }

    private static void VerReservaciones()
    {
        try
        {
            using (MySqlConnection connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();

                string query = @"SELECT Id, EventoId, Cliente, AsientosReservados, FechaReserva FROM reservaciones;";

                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        Console.WriteLine("\nListado de reservaciones:");
                        while (reader.Read())
                        {
                            int id = reader.GetInt32(0);
                            int eventoId = reader.GetInt32(1);
                            string cliente = reader.GetString(2);
                            int asientosReservados = reader.GetInt32(3);
                            DateTime fechaReserva = reader.GetDateTime(4);

                            Console.WriteLine($"ID: {id}, Evento ID: {eventoId}, Cliente: {cliente}, Asientos: {asientosReservados}, Fecha: {fechaReserva}");
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
            Console.WriteLine($"Asientos disponibles: {asientosDisponibles}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al consultar la disponibilidad: {ex.Message}");
        }
    }

    private static async Task<int> ConsultarDisponibilidadAsientos(int eventoId)
    {
        using (MySqlConnection connection = new MySqlConnection(ConnectionString))
        {
            await connection.OpenAsync();

            string query = "SELECT AsientosDisponibles FROM eventos WHERE Id = @EventoId;";

            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@EventoId", eventoId);

                object result = await command.ExecuteScalarAsync();

                if (result != null)
                {
                    return Convert.ToInt32(result);
                }
                else
                {
                    throw new Exception("Evento no encontrado.");
                }
            }
        }
    }

private static string GenerarBoletoPDF(string cliente, int eventoId, int asientos)
{
    string fileName = $"Boleto_{cliente}_{eventoId}.pdf";
    string filePath = Path.Combine(Environment.CurrentDirectory, fileName);

    using (PdfDocument pdf = new PdfDocument())
    {
        pdf.Info.Title = "Boleto de Reservación";

        PdfPage page = pdf.AddPage();
        XGraphics gfx = XGraphics.FromPdfPage(page);
        XFont font = new XFont("Arial", 12, XFontStyle.Regular);

        // Alineación del título en el centro superior de la página (XStringFormats.TopCenter)
        gfx.DrawString("Boleto de Reservación", font, XBrushes.Black, 
                       new XRect(0, 0, page.Width, 30), XStringFormats.TopCenter);

        // Usar una alineación segura sin BaseLine
        gfx.DrawString($"Cliente: {cliente}", font, XBrushes.Black, new XRect(20, 50, page.Width - 40, 20), XStringFormats.TopLeft);
        gfx.DrawString($"Evento ID: {eventoId}", font, XBrushes.Black, new XRect(20, 70, page.Width - 40, 20), XStringFormats.TopLeft);
        gfx.DrawString($"Asientos reservados: {asientos}", font, XBrushes.Black, new XRect(20, 90, page.Width - 40, 20), XStringFormats.TopLeft);
        gfx.DrawString($"Fecha: {DateTime.Now}", font, XBrushes.Black, new XRect(20, 110, page.Width - 40, 20), XStringFormats.TopLeft);

        pdf.Save(filePath);
    }

    return filePath;
}

}
		
