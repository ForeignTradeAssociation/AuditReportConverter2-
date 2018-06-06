using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using System.ComponentModel;
using System.Collections;
using System.Collections.ObjectModel;

namespace AuditReportConverter
{
    static class auditReportConverter
    {
        static public char OriginalDelimiter { get; set; }
        static public char DestinationDelimiter { get; set; }
        static public string SourceFile { get; set; }
        static public string DestinationFolder { get; set; }
        static private double NbLinesToProcess { get; set; }

        internal static int CalcProgress(double currentLineCounter)
        {
            return (int)Math.Round((double)(100 * currentLineCounter / NbLinesToProcess));
        }

        /// <summary>
        /// Change delimiter and cleanup newlines, quotes in strings...
        /// </summary>
        /// <param name="inputFile"></param>
        /// <param name="outputFile"></param>
        /// <param name="originalDelimiter"></param>
        /// <param name="destinationDelimiter"></param>
        internal static void ProcessFile(BackgroundWorker backGroundWorker)
        {
            NbLinesToProcess = File.ReadLines(SourceFile).Count();
            
            Console.WriteLine("Change delimiter");
            string inputFile = SourceFile;
            string outputFile = DestinationFolder + @"\\tmp_pass1.csv";
            char originalDelimiter = OriginalDelimiter;
            char destinationDelimiter = DestinationDelimiter;

            using (StreamReader sr = File.OpenText(inputFile))
            {
                using (StreamWriter sw = File.CreateText(outputFile))
                {
                    string dataLine = null;
                    List<string> dataFields = new List<string>();
                    Dictionary<string, int> companyCounter = new Dictionary<string, int>();

                    int nbLinesRead = 0;
                    int lastLine = 0;
                    int totalSkipped = 0;
                    int nbDataFields = 0;

                    // Concatenate split fields that are inside string definition
                    string splitField = "";
                    bool isSplitField = false;

                    while ((dataLine = sr.ReadLine()) != null)
                    {
                        nbLinesRead++;
                        backGroundWorker.ReportProgress(CalcProgress(nbLinesRead));

                        if (nbLinesRead == 1)
                        {
                            dataFields = new List<string>();
                        }
                        else
                        {
                            if (dataFields.Count >= nbDataFields)
                            {
                                dataFields = new List<string>();
                            }
                        }

                        // Split line into fields based upon original Delimiter
                        if (dataLine.Trim().Length != 0)
                        {
                            dataLine = dataLine.Replace("\"\"", "'");

                            string[] workFields = dataLine.Split(originalDelimiter);

                            foreach (string item in workFields)
                            {
                                // Are in the process of handling a string field?
                                if (isSplitField)
                                {
                                    if ((item.Length > 0) && (item[item.Length - 1] == '"'))
                                    {
                                        dataFields.Add(splitField + originalDelimiter + item);
                                        splitField = "";
                                        isSplitField = false;
                                    }
                                    else
                                    {
                                        // Add portion to previous part
                                        splitField += originalDelimiter + item;
                                    }
                                }
                                else
                                {
                                    // Check if we are in string
                                    if (item[0] == '"')
                                    {
                                        // Check if item is complete field
                                        if ((item.Length != 1) && (item[item.Length - 1] == '"'))
                                        {
                                            dataFields.Add(item);
                                        }
                                        else
                                        {
                                            splitField = item;
                                            isSplitField = true;
                                        }
                                    }
                                    else
                                    {
                                        dataFields.Add(item);
                                    }
                                }
                            }
                        }

                        if (nbLinesRead == 1)
                        {
                            nbDataFields = dataFields.Count;
                        }

                        if (dataFields.Count == nbDataFields)
                        {
                            totalSkipped += (nbLinesRead - lastLine - 1);
                            lastLine = nbLinesRead;
                            int i = 0;
                            string outputLine = string.Empty;
                            foreach (string field in dataFields)
                            {
                                i++;
                                outputLine += field.Replace(destinationDelimiter, originalDelimiter) + destinationDelimiter;
                            }
                            sw.WriteLine(outputLine.Substring(0, outputLine.Length - 1));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Transform to correct output based on field definitions
        /// </summary>
        /// <param name="delimiter"></param>
        /// <param name="inputFile"></param>
        /// <param name="outputFile"></param>
        internal static void TransformFile(BackgroundWorker backgroundWorker)
        {
            char delimiter = DestinationDelimiter;
            string inputFile = DestinationFolder + @"\\tmp_pass1.csv";
            string outputFile = DestinationFolder + @"\\tmp_pass2.csv";

            using (StreamReader sr = File.OpenText(inputFile))
            {
                using (StreamWriter sw = File.CreateText(outputFile))
                {
                    string dataLine = null;
                    List<string> dataFields = new List<string>();

                    int nbLinesRead = 0;
                    ArrayList dataFieldNames = new ArrayList();

                    while ((dataLine = sr.ReadLine()) != null)
                    {
                        nbLinesRead++;
                        backgroundWorker.ReportProgress(CalcProgress(nbLinesRead));
                        dataFields = new List<string>();

                        string[] workFields = dataLine.Split(delimiter);

                        int fieldNb = 0;

                        foreach (string item in workFields)
                        {
                            // Read & save data field names
                            if (nbLinesRead == 1)
                            {
                                dataFieldNames.Add(item.Replace("\"", string.Empty));
                                dataFields.Add(item);
                            }
                            else
                            {
                                // Process config information
                                string[] dateFields = new string[] { "Days in Audit StartDate", "Days in Audit EndDate", "Days in Original Submission Date" };

                                if (Array.Exists(dateFields, match: fieldName => fieldName == dataFieldNames[fieldNb].ToString()))
                                {
                                    dataFields.Add(DateTime.ParseExact(item.Replace("\"", string.Empty), "M/d/y", System.Globalization.CultureInfo.InvariantCulture).ToString("yyyy-MM-dd"));

                                }
                                else
                                {
                                    dataFields.Add(item);
                                }
                            }

                            fieldNb++;
                        }

                        // Write output
                        string outputLine = string.Empty;
                        foreach (string field in dataFields)
                        {
                            outputLine += field + delimiter;
                        }
                        sw.WriteLine(outputLine.Substring(0, outputLine.Length - 1));
                    }
                }
            }
        }

        internal static void ReportFile(BackgroundWorker backgroundWorker)
        {
            char delimiter = DestinationDelimiter;
            string inputFile = DestinationFolder + @"\\tmp_pass2.csv";
            string outputFile = DestinationFolder + @"\\report.txt";

            using (StreamReader sr = File.OpenText(inputFile))
            {
                string dataLine = null;
                List<string> dataFields = new List<string>();
                SortedDictionary<string, int> reportCounter = new SortedDictionary<string, int>();
                SortedDictionary<string, SortedDictionary<string, int>> monthlyReportCounter = new SortedDictionary<string, SortedDictionary<string, int>>();

                SortedList monthHeaders = new SortedList();

                int nbLinesRead = 0;
                ArrayList dataFieldNames = new ArrayList();

                while ((dataLine = sr.ReadLine()) != null)
                {
                    nbLinesRead++;
                    dataFields = new List<string>();

                    string[] workFields = dataLine.Split(delimiter);

                    if (nbLinesRead > 1)
                    {
                        if (reportCounter.ContainsKey(workFields[0]))
                        {
                            reportCounter[workFields[0]]++;
                        }
                        else
                        {
                            reportCounter[workFields[0]] = 1;
                        }

                        string reportingDate = DateTime.ParseExact(workFields[10], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture).ToString("yyyy-MM");

                        if (!monthHeaders.ContainsKey(reportingDate)) monthHeaders.Add(reportingDate, 0);

                        if (monthlyReportCounter.ContainsKey(workFields[0]))
                        {
                            if (monthlyReportCounter[workFields[0]].ContainsKey(reportingDate))
                            {
                                monthlyReportCounter[workFields[0]][reportingDate]++;
                            }
                            else
                            {
                                monthlyReportCounter[workFields[0]][reportingDate] = 1;
                            }
                        }
                        else
                        {
                            monthlyReportCounter[workFields[0]] = new SortedDictionary<string, int>();
                            monthlyReportCounter[workFields[0]].Add(reportingDate, 1);
                        }

                    }
                }

                // Write output to console
                using (StreamWriter sw = File.CreateText(outputFile))
                {
                    sw.WriteLine(string.Format("Process Report ({0:dd-MM-yyyy HH:mm:ss})", DateTime.Now));
                    sw.WriteLine();

                    //int sumLines = 0;

                    //foreach (KeyValuePair<string, int> kvp in reportCounter)
                    //{
                    //    sw.WriteLine(String.Format("{0,25} - {1,10}", new string(kvp.Key.Take(25).ToArray()), kvp.Value));
                    //    sumLines = sumLines + kvp.Value;
                    //}
                    //sw.WriteLine("Total of {0} lines", sumLines);
                    //sw.WriteLine();

                    // *** Output in table form ***
                    int sumMonthLines = 0;
                    int sumTotalLines = 0;
                    SortedList monthTotals = (SortedList)monthHeaders.Clone();

                    sw.Write(String.Format("{0,-25}\t", string.Empty));

                    foreach (string value in monthHeaders.Keys)
                    {
                        sw.Write(String.Format("{0,7}\t", value));
                    }

                    sw.WriteLine(String.Format("{0,7}", "Total"));

                    sw.Write(String.Format("{0,-25}\t", new String('-', 25)));

                    foreach (string value in monthHeaders.Keys)
                    {
                        sw.Write(String.Format("{0,7}\t", new String('-', 7)));
                    }

                    sw.WriteLine(String.Format("{0,7}", new String('-', 7)));

                    foreach (KeyValuePair<string, SortedDictionary<string, int>> kvp in monthlyReportCounter)
                    {
                        sumMonthLines = 0;
                        SortedList companyPerMonth = (SortedList)monthHeaders.Clone();

                        foreach (KeyValuePair<string, int> detailkvp in kvp.Value)
                        {
                            sumMonthLines += detailkvp.Value;
                            companyPerMonth[detailkvp.Key] = detailkvp.Value;
                            monthTotals[detailkvp.Key] = (int)monthTotals[detailkvp.Key] + detailkvp.Value;
                        }
                        sumTotalLines += sumMonthLines;

                        sw.Write(String.Format("{0,-25}\t", new string(kvp.Key.Trim('"').Take(25).ToArray())));

                        foreach (int value in companyPerMonth.Values)
                        {
                            sw.Write(String.Format("{0,7}\t", value));
                        }

                        sw.WriteLine(String.Format("{0,7}", sumMonthLines));
                    }
                    sw.Write(String.Format("{0,-25}\t", new String('=', 25)));

                    foreach (string value in monthHeaders.Keys)
                    {
                        sw.Write(String.Format("{0,7}\t", new String('=', 7)));
                    }

                    sw.WriteLine(String.Format("{0,7}", new String('=', 7)));
                    sw.Write(String.Format("{0,25}\t", "Totals"));

                    foreach (int value in monthTotals.Values)
                    {
                        sw.Write(String.Format("{0,7}\t", value));
                    }
                    sw.WriteLine(String.Format("{0,7}", sumTotalLines));
                }
            }
        }

        internal static void SplitFile(BackgroundWorker backgroundWorker)
        {
            char delimiter = DestinationDelimiter;
            string inputFile = DestinationFolder + @"\\tmp_pass2.csv";

            using (StreamReader sr = File.OpenText(inputFile))
            {
                string dataLine = null;
                Dictionary<string, int> splitFiles = new Dictionary<string, int>();

                string headerLine = string.Empty;

                int nbLinesRead = 0;

                while ((dataLine = sr.ReadLine()) != null)
                {
                    nbLinesRead++;

                    backgroundWorker.ReportProgress(CalcProgress(nbLinesRead));

                    if (nbLinesRead == 1)
                    {
                        headerLine = dataLine;
                    }
                    else
                    {
                        string companyName = dataLine.Split(delimiter)[0].Substring(1, dataLine.Split(delimiter)[0].Length - 2);
                        string fileName = DestinationFolder + @"\\_" + companyName + ".csv";
                        Collection<String> outputLines = new Collection<String>();

                        if (!splitFiles.ContainsKey(companyName))
                        {
                            outputLines.Add(headerLine);
                            splitFiles.Add(companyName, 0);
                        }
                        else
                        {
                            splitFiles[companyName]++;
                        }
                        outputLines.Add(dataLine);

                        File.AppendAllLines(fileName, outputLines);
                    }
                }

                // Write output to console
                int sumLines = 0;
                foreach (KeyValuePair<string, int> kvp in splitFiles)
                {
                    Console.WriteLine(String.Format("{0,25} - {1,10}", new string(kvp.Key.Take(25).ToArray()), kvp.Value));
                    sumLines = sumLines + kvp.Value;
                }
                Console.WriteLine("Total of {0} lines", sumLines);
                Console.WriteLine();
            }
        }
    }
}