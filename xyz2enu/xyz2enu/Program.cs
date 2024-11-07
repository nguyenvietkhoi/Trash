using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Globalization;

namespace xyz2enu
{
    class Program
    {
        public static (double E, double N, double U) XYZtoENU(double x, double y, double z, double refX, double refY, double refZ)
        {
            if (Double.IsNaN(x) || Double.IsNaN(y) || Double.IsNaN(z))
                return (Double.NaN, Double.NaN, Double.NaN);

            // Compute the latitude and longitude from the reference ECEF coordinates
            double refLat = Math.Asin(refZ / Math.Sqrt(refX * refX + refY * refY + refZ * refZ)) * 180.0 / Math.PI;
            double refLon = Math.Atan2(refY, refX) * 180.0 / Math.PI;

            // Convert reference latitude and longitude from degrees to radians
            double latRad = refLat * Math.PI / 180.0;
            double lonRad = refLon * Math.PI / 180.0;

            // Compute the differences in ECEF coordinates
            double dx = x - refX;
            double dy = y - refY;
            double dz = z - refZ;

            // Compute ENU coordinates
            double e = -Math.Sin(lonRad) * dx + Math.Cos(lonRad) * dy;
            double n = -Math.Sin(latRad) * Math.Cos(lonRad) * dx - Math.Sin(latRad) * Math.Sin(lonRad) * dy + Math.Cos(latRad) * dz;
            double u = Math.Cos(latRad) * Math.Cos(lonRad) * dx + Math.Cos(latRad) * Math.Sin(lonRad) * dy + Math.Sin(latRad) * dz;

            return (e, n, u);
        }

        static Double CalculateMean(Double[,] array, int columnIndex)
        {
            int rowCount = array.GetLength(0);  // Get the number of rows
            double sum = 0;
            int count = 0;

            for (int i = 0; i < rowCount; i++)
            {
                double value = array[i, columnIndex];
                if (!double.IsNaN(value))  // Ignore NaN values
                {
                    sum += value;
                    count++;
                }                
            }

            return count > 0 ? sum / count : double.NaN;
        }

        static void Main(string[] args)
        {
            // Check if the user provided exactly three file paths
            if (args.Length != 1)
            {
                Console.WriteLine("Please provide exactly three file paths as arguments.");
                return;
            }

            using (StreamReader sr = new StreamReader(args[0]))
            {
                string csvline = sr.ReadLine();
                while ((csvline = sr.ReadLine()) != null)
                {
                    // Split the line by commas (assuming no commas within fields)
                    string[] fields = csvline.Split(',');

                    // Ensure there are exactly 4 fields (if needed)
                    if (fields.Length == 4)
                    {
                        string stationname = fields[0];
                        Double X = Double.Parse(fields[1], CultureInfo.InvariantCulture);
                        Double Y = Double.Parse(fields[2], CultureInfo.InvariantCulture);
                        Double Z = Double.Parse(fields[3], CultureInfo.InvariantCulture);
                        int year = 0;

                        Double[,] TXYZ_list = new Double[366,4];

                        // Initialize each list in the array
                        for (int i = 0; i < 366; i++)
                        {
                            TXYZ_list[i, 0] = 0.5 / 365.0 + i / 365.0;
                            for (int j = 1; j < 4; j++)
                            {
                                TXYZ_list[i, j] = Double.NaN;
                            }
                        }

                        // Iterate through each provided file path
                        for (int i = 1; i < 4; i++)
                        {
                            string filePath = "mb_" + stationname + "_XYZ.dat" + i.ToString();
                            try
                            {
                                // Read and display the contents of the file
                                string content = File.ReadAllText(filePath);

                                using (StreamReader reader = new StreamReader(filePath))
                                {
                                    string line;

                                    //Read header
                                    line = reader.ReadLine();
                                    line = reader.ReadLine();
                                    line = reader.ReadLine();

                                    while ((line = reader.ReadLine()) != null)
                                    {
                                        // Split the input string by whitespace and convert to float
                                        string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                        int rowid;

                                        if (parts.Length < 3)
                                        {
                                            Console.WriteLine("Not enough numbers provided.");
                                            return;
                                        }

                                        Double T = Double.Parse(parts[0], CultureInfo.InvariantCulture);
                                        year = (int)T;
                                        rowid = (int)Math.Round(((T % 1) - (1.0 / (365.0 * 2))) * 365);

                                        TXYZ_list[rowid, i] = Double.Parse(parts[1], CultureInfo.InvariantCulture);
                                    }
                                }
                            }
                            catch (FileNotFoundException)
                            {
                                Console.WriteLine($"File not found: {filePath}");
                            }
                            catch (UnauthorizedAccessException)
                            {
                                Console.WriteLine($"Access denied to file: {filePath}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"An error occurred while reading {filePath}: {ex.Message}");
                            }
                        }

                        string outputfile = stationname + ".gta";
                        string outputfile2 = stationname + "_year.gta";
                        try
                        {
                            File.WriteAllText(outputfile, "#Site:" + stationname + Environment.NewLine);
                            File.WriteAllText(outputfile2, "#Site:" + stationname + Environment.NewLine);

                            double refX = CalculateMean(TXYZ_list, 1);
                            double refY = CalculateMean(TXYZ_list, 2);
                            double refZ = CalculateMean(TXYZ_list, 3);

                            File.AppendAllText(outputfile, "#X:" + refX.ToString($"F4", CultureInfo.InvariantCulture).PadLeft(14) + Environment.NewLine);
                            File.AppendAllText(outputfile, "#Y:" + refY.ToString($"F4", CultureInfo.InvariantCulture).PadLeft(14) + Environment.NewLine);
                            File.AppendAllText(outputfile, "#Z:" + refZ.ToString($"F4", CultureInfo.InvariantCulture).PadLeft(14) + Environment.NewLine);
                            File.AppendAllText(outputfile, "#   T         N        E        U" + Environment.NewLine);

                            File.AppendAllText(outputfile2, "#X:" + refX.ToString($"F4", CultureInfo.InvariantCulture).PadLeft(14) + Environment.NewLine);
                            File.AppendAllText(outputfile2, "#Y:" + refY.ToString($"F4", CultureInfo.InvariantCulture).PadLeft(14) + Environment.NewLine);
                            File.AppendAllText(outputfile2, "#Z:" + refZ.ToString($"F4", CultureInfo.InvariantCulture).PadLeft(14) + Environment.NewLine);
                            File.AppendAllText(outputfile2, "#   T         N        E        U" + Environment.NewLine);

                            for (int j = 0; j < 366; j++)
                            {
                                var enu = XYZtoENU(TXYZ_list[j, 1], TXYZ_list[j, 2], TXYZ_list[j, 3], X, Y, Z);
                                File.AppendAllText(outputfile, (0.5 + j + (year + 1) * 365.0).ToString($"F4", CultureInfo.InvariantCulture).PadLeft(11) + " " + string.Format(CultureInfo.InvariantCulture, "{0,7:0.0000}", enu.E) + " " + string.Format(CultureInfo.InvariantCulture, "{0,7:0.0000}", enu.N) + " " + string.Format(CultureInfo.InvariantCulture, "{0,7:0.0000}", enu.U) + Environment.NewLine);
                                File.AppendAllText(outputfile2, (0.5 / 365.0 + j / 365.0 + year).ToString($"F4", CultureInfo.InvariantCulture).PadLeft(9) + " " + string.Format(CultureInfo.InvariantCulture, "{0,7:0.0000}", enu.E) + " " + string.Format(CultureInfo.InvariantCulture, "{0,7:0.0000}", enu.N) + " " + string.Format(CultureInfo.InvariantCulture, "{0,7:0.0000}", enu.U) + Environment.NewLine);
                            }
                            Console.WriteLine("Successfully.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"An error occurred: {ex.Message}");
                        }


                    }
                    else
                    {
                        Console.WriteLine("Invalid line, expected 4 fields.");
                    }
                }
            }
        }
    }
}
