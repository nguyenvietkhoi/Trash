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

        static Double CalculateMean(List<Double> list)
        {
            if (list.Count == 0)
            {
                throw new InvalidOperationException("Cannot calculate mean of an empty list.");
            }

            Double sum = 0.0;

            foreach (var value in list)
            {
                sum += value;
            }

            return sum / list.Count;
        }

        static void Main(string[] args)
        {
            // Check if the user provided exactly three file paths
            if (args.Length != 6)
            {
                Console.WriteLine("Please provide exactly three file paths as arguments.");
                return;
            }

            string stationname = Path.GetFileName(args[0]).Substring(3, 4);
            List<Double> T_list = new List<Double>();
            List<Double>[] XYZ_list = new List<Double>[3];

            // Initialize each list in the array
            for (int i = 0; i < XYZ_list.Length; i++)
            {
                XYZ_list[i] = new List<Double>();
            }

            // Iterate through each provided file path
            for (int i = 0; i < 3; i++)
            {
                string filePath = args[i];
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

                            if (parts.Length < 3)
                            {
                                Console.WriteLine("Not enough numbers provided.");
                                return;
                            }

                            // Parse the first three numbers
                            if (i == 0)
                            {
                                T_list.Add(Double.Parse(parts[0], CultureInfo.InvariantCulture));
                            }
                            XYZ_list[i].Add(Double.Parse(parts[1], CultureInfo.InvariantCulture));
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

                //double refX = CalculateMean(XYZ_list[0]);
                //double refY = CalculateMean(XYZ_list[1]);
                //double refZ = CalculateMean(XYZ_list[2]);

                double refX = Double.Parse(args[3], CultureInfo.InvariantCulture);
                double refY = Double.Parse(args[4], CultureInfo.InvariantCulture);
                double refZ = Double.Parse(args[5], CultureInfo.InvariantCulture);

                File.AppendAllText(outputfile, "#X:" + refX.ToString($"F4", CultureInfo.InvariantCulture).PadLeft(14) + Environment.NewLine);
                File.AppendAllText(outputfile, "#Y:" + refY.ToString($"F4", CultureInfo.InvariantCulture).PadLeft(14) + Environment.NewLine);
                File.AppendAllText(outputfile, "#Z:" + refZ.ToString($"F4", CultureInfo.InvariantCulture).PadLeft(14) + Environment.NewLine);
                File.AppendAllText(outputfile, "#   T         N        E        U" + Environment.NewLine);

                File.AppendAllText(outputfile2, "#X:" + refX.ToString($"F4", CultureInfo.InvariantCulture).PadLeft(14) + Environment.NewLine);
                File.AppendAllText(outputfile2, "#Y:" + refY.ToString($"F4", CultureInfo.InvariantCulture).PadLeft(14) + Environment.NewLine);
                File.AppendAllText(outputfile2, "#Z:" + refZ.ToString($"F4", CultureInfo.InvariantCulture).PadLeft(14) + Environment.NewLine);
                File.AppendAllText(outputfile2, "#   T         N        E        U" + Environment.NewLine);

                for (int j = 0; j < T_list.Count; j++)
                {
                    var enu = XYZtoENU(XYZ_list[0][j], XYZ_list[1][j], XYZ_list[2][j], refX, refY, refZ);
                    File.AppendAllText(outputfile, (T_list[j] * 365).ToString($"F4", CultureInfo.InvariantCulture).PadLeft(11) + (enu.E * 1000).ToString($"F4", CultureInfo.InvariantCulture).PadLeft(8) + (enu.N * 1000).ToString($"F4", CultureInfo.InvariantCulture).PadLeft(8) + (enu.U * 1000).ToString($"F4", CultureInfo.InvariantCulture).PadLeft(8) + Environment.NewLine);
                    File.AppendAllText(outputfile2, T_list[j].ToString($"F4", CultureInfo.InvariantCulture).PadLeft(11) + (enu.E * 1000).ToString($"F4", CultureInfo.InvariantCulture).PadLeft(8) + (enu.N * 1000).ToString($"F4", CultureInfo.InvariantCulture).PadLeft(8) + (enu.U * 1000).ToString($"F4", CultureInfo.InvariantCulture).PadLeft(8) + Environment.NewLine);
                }
                Console.WriteLine("Successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

        }
    }
}
