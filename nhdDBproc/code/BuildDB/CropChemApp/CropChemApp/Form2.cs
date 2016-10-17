using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Data.OleDb;
using System.Data.SQLite;
using System.Globalization;

namespace CropChemApp
{
    public partial class Form2 : Form
    {
        //private string dbFile = "PISCESAttributes.db";
        private string dbFile = "NHDPlusv2Attributes.db";
        private string procreport = "ProcessingReport.txt";
        private StringBuilder sbrpt;  //for writing a db processing report on disk

        //utility reads user-selected csv/dbf nhdplusv2 attribute files (currently precip, elevslope, DA, sfVelocity) and
        //  1 - sequentially creates and/or appends pertinent data to csv files on disk 
        //  2 - as an intermediate step, sequentially reads those csv files back into memory prior to
        //  3 - sequentially creating sqlite data tables in the piscesattribute db

        //forget that - create all csv files for all regions, store on disk
        //read a region's csv files together, merge by comid into table
        //write to a single db table
        //repeat until all regions done.

        public Form2()
        {
            InitializeComponent();
            if (CreatePiSCESDB()) CreateNhdAttributeTable();
            //CreateNhdAttributeTable();
        }

        private bool CreatePiSCESDB()
        {
            //create the db
            //string dbFile = "PISCESAttributes.s3db";
            DialogResult ans = MessageBox.Show("Create sqlitedb?  No to preserve existing, Yes to delete and re-create.", "Create DB", MessageBoxButtons.YesNo);
            if (ans == DialogResult.Yes)
            {
                if (File.Exists(dbFile))
                    File.Delete(dbFile);

                if (File.Exists(dbFile))
                    SQLiteConnection.CreateFile(dbFile);
                return true;
            }
            return false;
        }

        private void CreateNhdAttributeTable()
        {
            SQLiteConnection dbConnection = new SQLiteConnection(string.Format("Data Source={0};Version=3;", dbFile));
            dbConnection.Open();
            IDbTransaction transaction1 = dbConnection.BeginTransaction();
            string tableName = string.Empty;
            //create the table we want
            string sql = GetCreateSqlTable("combined", out tableName);
            SQLiteCommand command = new SQLiteCommand(sql, dbConnection);
            command.ExecuteNonQuery();
            transaction1.Commit();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //creating csv files
            string file1 = string.Empty;

            string sheet1FN = BrwsCSVFile("Find datafile to process.");
            if (Path.GetExtension(sheet1FN).Contains("csv") || Path.GetExtension(sheet1FN).Contains("txt"))
            {
                file1 = ReadCSVFile(sheet1FN);
                writeCSVPrecipTableFile(file1, sheet1FN);
            }
            else if (Path.GetExtension(sheet1FN).Contains("dbf"))
            {
                string path = Path.GetFullPath(sheet1FN);
                //if (path.Contains("elevslope"))
                //    writeCSVfromDBFFile(sheet1FN);
                //else if (path.Contains("CumulativeArea"))
                //    writeCSVfromDBFFile(sheet1FN);
                writeCSVfromDBFFile(sheet1FN);
            }

        }

        private void button2_Click(object sender, EventArgs e)
        {
            string sheet1FN = BrwsCSVFile("Find csv to process.");
            string fileName = Path.GetFileNameWithoutExtension(sheet1FN).ToString();
            label1.Text = "Writing sqlite table from " + fileName;
            Application.DoEvents();
            //writeSQLTable(sheet1FN);
            label1.Text = "Done with " + fileName;
        }

        private string BrwsCSVFile(string title)
        {
            string filename = string.Empty;
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = title;
            ofd.Multiselect = false;

            if (ofd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                throw new Exception("gotta have the file - try again: " + title.ToString());

            return ofd.FileName;
        }

        private string ReadCSVFile(string filename)
        {
            //load the source file into memory
            string file = string.Empty;
            StreamReader sr = new StreamReader(filename);
            file = sr.ReadToEnd();
            sr.Close();
            return file;

        }

        private void writeCSVPrecipTableFile(string file1, string sheet1FN)
        {
            //input is a text or csv file...
            //write a csv file suitable for import into sqlite
            label1.Text = "Working " + sheet1FN.ToString() + "...";
            string outfile = string.Empty;

            //break files into lines
            string[] lines = file1.Split('\n');


            //append to existing or create new csv?

            DialogResult ans = MessageBox.Show("Crate pisces.csv?", "NO if appending precip data?", MessageBoxButtons.YesNo);
            //create header line with COMID and PrecipCat (but only if creating new precip.csv)
            if (ans == DialogResult.Yes)
            {
                string[] h1 = lines[0].Split(',');
                string header = h1[0] + "," + h1[3] + "\n";
                outfile = outfile + header;
            }

            int ctr = 0;
            for (int line = 1; line < lines.Length - 1; line++)
            {
                string[] cols = lines[line].Split(',');
                if (Convert.ToDouble(cols[0].ToString()) <= 0) continue;
                outfile = outfile + cols[0] + "," + cols[3] + "\n";
                Console.WriteLine("lines read:" + ctr++.ToString());
            }

            //append to existing or create new csv?
            if (ans == DialogResult.No)
            {
                //open existing and append outfile
                StreamReader sr = new StreamReader("precip.csv");
                string infile = sr.ReadToEnd();
                sr.Close();
                outfile = infile + outfile;
            }

            string currentLoc = Application.ExecutablePath;
            string currentPath = Path.GetDirectoryName(currentLoc);
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.SelectedPath = currentPath + @"\csvfiles\";
            fbd.ShowDialog();
            string path = fbd.SelectedPath.ToString();
            string fileName = path + "\\" + "precip.csv";
            StreamWriter sw = new StreamWriter(fileName);
            sw.Write(outfile);
            sw.Close();

            label1.Text = "Done processing " + sheet1FN.ToString();

        }

        private void writeCSVfromDBFFile(string sheet1FN)
        {
            //input is a dbf file...

            label1.Text = "Working " + sheet1FN.ToString() + "...";
            DataTable dt = readDBFFile(sheet1FN); //read only data we want

            DialogResult ans = MessageBox.Show("Crate pisces.csv?", "NO if appending attribute data?", MessageBoxButtons.YesNo);

            StringBuilder sb = new StringBuilder();

            if (ans == DialogResult.Yes) //write the column headers to memory
            {
                string[] cols = dt.Columns.Cast<DataColumn>().Select(column => column.ColumnName).ToArray();
                sb.AppendLine(string.Join(",", cols));
            }

            foreach (DataRow dr in dt.Rows) //write the data to memory
            {
                string[] fields = dr.ItemArray.Select(field => field.ToString()).ToArray();
                sb.AppendLine(string.Join(",", fields));
            }

            string outfile = sb.ToString();

            string fileName = Path.GetFileNameWithoutExtension(sheet1FN).ToString() + ".csv";
            if (ans == DialogResult.No) //to append, read existing and append new data
            {
                StreamReader sr = new StreamReader(fileName);
                string infile = sr.ReadToEnd();
                sr.Close();
                outfile = infile + outfile;
            }

            string currentLoc = Application.ExecutablePath;
            string currentPath = Path.GetDirectoryName(currentLoc);
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.SelectedPath = currentPath + @"\csvfiles\";
            fbd.ShowDialog();
            string path = fbd.SelectedPath.ToString();
            fileName = path + "\\" + fileName;
            StreamWriter sw = new StreamWriter(fileName); //write data to csv file
            sw.Write(outfile);
            sw.Close();
            sb.Clear();

            label1.Text = "Done processing " + sheet1FN.ToString();

        }

        private DataTable readDBFFile(string sheet1FN)
        {
            //read a dbf file
            DataTable dt = new DataTable();

            string dataSrc = @"Data Source=" + sheet1FN + ";";
            OleDbConnection conn = new OleDbConnection(@"Provider=VFPOLEDB.1;" + dataSrc);
            conn.Open();
            //string sql = "select * from " + Path.GetFileNameWithoutExtension(sheet1FN).ToString();
            string sql = GetDTSql(sheet1FN); //get only data we want
            OleDbCommand command = new OleDbCommand(sql, conn);
            OleDbDataAdapter da = new OleDbDataAdapter(command);
            da.Fill(dt);

            conn.Close();
            da.Dispose();

            return dt; ;
        }

        private string GetDTSql(string sheet1FN)
        {
            //sql for populating datatable with only cols we want out of dbf files
            string sql = string.Empty;
            if (sheet1FN.Contains("elevslope"))
            {
                sql = "select comid, maxelevsmo, minelevsmo, slope from " + Path.GetFileNameWithoutExtension(sheet1FN).ToString();
            }
            else if (sheet1FN.Contains("CumulativeArea"))
            {
                sql = "select ComID, TotDASqKM from " + Path.GetFileNameWithoutExtension(sheet1FN).ToString();
            }
            else if (sheet1FN.Contains("Vogel"))
            {
                sql = "select COMID, MAFLOWV, MAVELV from " + Path.GetFileNameWithoutExtension(sheet1FN).ToString();
            }
            //else if (sheet1FN.Contains("combined"))
            //{
            //    sql = "";
            //}

            return sql;
        }

        //private void writeSQLTable(string csvFileName)
        private void writeSQLTable(DataTable dt)
        {
            SQLiteConnection dbConnection = new SQLiteConnection(string.Format("Data Source={0};Version=3;", dbFile));
            dbConnection.Open();

            sbrpt.AppendLine("*** Inserting records into db table ***");
            sbrpt.AppendLine("master datatable record count: " + dt.Rows.Count.ToString());

            IDbTransaction transaction1 = dbConnection.BeginTransaction();
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                StringBuilder sbColNames = new StringBuilder("insert into " + "NHDv21Attributes" + " (");
                StringBuilder sbValues = new StringBuilder(" values (");

                int count = 0;
                foreach (DataColumn dc in dt.Columns)
                {
                    if (count > 0)
                    {
                        sbColNames.Append(", ");
                        sbValues.Append(", ");
                    }

                    sbColNames.Append("[" + dc.ColumnName + "]");
                    sbValues.Append("'" + dt.Rows[i][dc].ToString() + "'");
                    count++;
                }
                sbColNames.Append(")");
                sbValues.Append(")");
                string sql = sbColNames.ToString() + sbValues.ToString();

                SQLiteCommand command = new SQLiteCommand(sql, dbConnection);
                command.ExecuteNonQuery();
            }

            transaction1.Commit();
            transaction1.Dispose();
            dbConnection.Close();
            sbrpt.AppendLine("db data records inserted: " + dt.Rows.Count.ToString());

            string infile = string.Empty;
            if (File.Exists(procreport))
            {
            StreamReader sr = new StreamReader(procreport);
            infile = sr.ReadToEnd();
            sr.Close();
            }
            sbrpt.AppendLine("");
            sbrpt.AppendLine("");
            string outfile = infile + sbrpt.ToString();
            StreamWriter sw = new StreamWriter(procreport);
            sw.Write(outfile);
            sw.Close();
            sw.Dispose();

            MessageBox.Show("Done", "AttaBoy", MessageBoxButtons.OK);
        }

        private void upDateSQLTable(string csvFileName)
        {
            SQLiteConnection dbConnection = new SQLiteConnection(string.Format("Data Source={0};Version=3;", dbFile));
            dbConnection.Open();

            //get the data we want to write into the db table
            DataTable dt = ReadAttrCSVFile(csvFileName);
            //insert records into table
            IDbTransaction transaction1 = dbConnection.BeginTransaction();
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                //StringBuilder sbsql = new StringBuilder("update NHDv21Attributes " + "set " + "(");
                StringBuilder sbsql = new StringBuilder("update NHDv21Attributes " + "set ");
                string comid = string.Empty;

                int count = 0;
                foreach (DataColumn dc in dt.Columns)
                {
                    if (count < 1)
                    {
                        comid = dt.Rows[i][dc].ToString();
                    }
                    else
                    {
                        //sbsql.Append("[" + dc.ColumnName + "]=");
                        sbsql.Append(dc.ColumnName + "=");
                        if (count <= dt.Columns.Count - 2)
                            sbsql.Append("'" + dt.Rows[i][dc].ToString() + "', ");
                        else
                            sbsql.Append("'" + dt.Rows[i][dc].ToString() + "'");

                    }
                    count++;
                }

                //sbsql.Append(")");

                string sql = sbsql.ToString() + " where COMID=" + comid + ";";

                SQLiteCommand command = new SQLiteCommand(sql, dbConnection);
                command.ExecuteNonQuery();
            }

            transaction1.Commit();
            transaction1.Dispose();
            dbConnection.Close();

            MessageBox.Show("Done", "AttaBoy", MessageBoxButtons.OK);
        }

        private DataTable ReadAttrCSVFile(string file)
        {
            //get a csv file into a datatable

            if (File.Exists(file) == false)
                throw new Exception("Could not find file: " + file);

            StreamReader sr = new StreamReader(file);
            //First line is headers
            string line = sr.ReadLine();
            string[] headers = line.Split(",".ToCharArray());

            //create columns
            DataTable dt = new DataTable();
            for (int i = 0; i < headers.Length; i++)
                dt.Columns.Add(headers[i]);

            //populate rows 
            while ((line = sr.ReadLine()) != null)
            {
                DataRow dr = dt.NewRow();
                string[] row = line.Split(",".ToCharArray());
                for (int i = 0; i < row.Length; i++)
                {
                    //row[i].Replace('"', ' ');
                    dr[headers[i]] = row[i];
                }
                dt.Rows.Add(dr);
            }

            sr.Close();

            return dt;

        }

        private string GetCreateSqlTable(string fileName, out string tableName)
        {
            //sql for creating table names we want
            string sql = string.Empty;
            tableName = string.Empty;

            if (fileName.Contains("precip"))
            {
                sql = @"CREATE TABLE [CatPrecip] (
                            [COMID] INT KEY NOT NULL,
                            [PrecipVC] REAL NULL
                            );";
                tableName = "CatPrecip";
            }
            else if (fileName.Contains("elevslop"))
            {
                sql = @"CREATE TABLE [ElevSlope] (
                            [COMID] INT KEY NOT NULL,
                            [MAXELEVSMO] REAL NULL,
                            [MINELEVSMO] REAL NULL,
                            [SLOPE] REAL NULL
                            );";
                tableName = "ElevSlope";
            }
            else if (fileName.Contains("CumulativeArea"))
            {
                sql = @"CREATE TABLE [CumDA] (
                            [COMID] INT KEY NOT NULL,
                            [TotDASqKM] REAL NULL
                            );";
                tableName = "CumDA";
            }
            else if (fileName.Contains("Vogel"))
            {
                sql = @"CREATE TABLE [VogelFlow] (
                        [COMID] INT KEY NOT NULL,
                        [MAFLOWV] REAL NULL,
                        [MAVELV] REAL NULL
                        );";
                tableName = "VogelFlow";
            }
            else if (fileName.Contains("combined"))
            {
                sql = @"CREATE TABLE [NHDv21Attributes] (
                            [COMID] INT KEY NOT NULL,
                            [TotDASqKM] REAL NULL,
                            [MAXELEVSMO] REAL NULL,
                            [MINELEVSMO] REAL NULL,
                            [SLOPE] REAL NULL,
                            [PrecipVC] REAL NULL,
                            [MAFLOWV] REAL NULL,
                            [MAVELV] REAL NULL
                            );";
                tableName = "NHDv21Attributes";
            }

            return sql;

        }

        private void button3_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            sbrpt = new StringBuilder();

            DataTable dtDA = new DataTable();
            DataTable dtElev = new DataTable();
            DataTable dtPrecip = new DataTable();
            DataTable dtFlow = new DataTable();

            string currentLoc = Application.ExecutablePath;
            string currentPath = Path.GetDirectoryName(currentLoc);
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.SelectedPath = currentPath + @"\csvfiles\";
            fbd.ShowDialog();
            string path = fbd.SelectedPath.ToString();
            string region = Path.GetFileName(path);

            string[] files = Directory.GetFiles(path, "*.csv");
            sbrpt.AppendLine("*** Reading csv files, region: " + region + " ***");
            label1.Text = "reading csv files for " + region;
            Application.DoEvents();
            foreach (string file in files)
            {
                string filename = Path.GetFileNameWithoutExtension(file);
                if (string.Equals(filename, "CumulativeArea", StringComparison.OrdinalIgnoreCase))
                    dtDA = GetDataTableFromCsv(file, true);
                else if (string.Equals(filename, "elevslope", StringComparison.OrdinalIgnoreCase))
                    dtElev = GetDataTableFromCsv(file, true);
                else if (string.Equals(filename, "precip", StringComparison.OrdinalIgnoreCase))
                    dtPrecip = GetDataTableFromCsv(file, true);
                else if (string.Equals(filename, "VogelFlow", StringComparison.OrdinalIgnoreCase))
                    dtFlow = GetDataTableFromCsv(file, true);
            }

            label1.Text = "merging csv files for " + region;
            Application.DoEvents();
            sbrpt.AppendLine("*** Merging csv data for region: " + region + " ***");
            DataTable mergedDT = mergeDTs(dtDA, dtElev, dtPrecip, dtFlow);

            label1.Text = "writing data to sqlite table for " + region;
            Application.DoEvents();
            sbrpt.AppendLine("*** Writing to DB for region: " + region + " ***");
            writeSQLTable(mergedDT);
            label1.Text = "Done with " + path;


            //string sheet1FN = BrwsCSVFile("Find csv to process.");
            //string fileName = Path.GetFileNameWithoutExtension(sheet1FN).ToString();
            //label1.Text = "Done with " + path;
            //Application.DoEvents();
            //DialogResult ans = MessageBox.Show("Yes to INSERT records, No to UPDATE records in table?", "INSERT or UPDATE?", MessageBoxButtons.YesNo);
            //if (ans == DialogResult.Yes)
            //{
            //    writeSQLTable(sheet1FN);
            //}
            //else
            //{
            //    upDateSQLTable(sheet1FN);
            //}
        }

        private DataTable mergeDTs(DataTable dtDA, DataTable dtElev, DataTable dtPrecip, DataTable dtFlow)
        {
            //throw new NotImplementedException();
            DataTable dt = createMasterDT();
            int recsprocessed = 0;

            sbrpt.AppendLine("*** Merging datatables ***");
            sbrpt.AppendLine("Precip table record count: " + dtPrecip.Rows.Count.ToString());
            sbrpt.AppendLine("Flow table record count: " + dtFlow.Rows.Count.ToString());
            sbrpt.AppendLine("ElevSlope table record count: " + dtElev.Rows.Count.ToString());   
            sbrpt.AppendLine("DA table record count: " + dtDA.Rows.Count.ToString());
            sbrpt.AppendLine("iterating over precip table...");
            

            //flow and precip tables have common number of nhd records, DA and elevslope table may have more so...
            //iterate over either precip or flow and select rows by comids from other tables, merge into master table
            int mergeErrCount = 0;
            for (int r = 0; r < dtPrecip.Rows.Count; r++)
            {
                //string comid = dtPrecip.Columns[0].ColumnName.ToString() + " = " + dtPrecip.Rows[r][0].ToString();
                string comid = "comid" + " = " + dtPrecip.Rows[r][0].ToString();

                //should only be 1
                DataRow[] rowsPrecip = dtPrecip.Select(comid);
                if (rowsPrecip.Length != 1)
                {
                    mergeErrCount++;
                    if (rowsPrecip.Length > 1)
                        sbrpt.AppendLine("more than one entry in precip for comid= " + comid);
                }

                //should only be 1
                DataRow[] rowsElev = dtElev.Select(comid);
                if (rowsElev.Length != 1)
                {
                    mergeErrCount++;
                    if (rowsElev.Length > 1)
                        sbrpt.AppendLine("more than one entry in elevslope for comid= " + comid);
                    else if (rowsElev.Length < 0)
                        sbrpt.AppendLine("no matching entry in elevslope for comid= " + comid);
                }

                //should only be 1
                DataRow[] rowsFlow = dtFlow.Select(comid);
                if (rowsFlow.Length != 1)
                {
                    mergeErrCount++;
                    if (rowsFlow.Length > 1)
                        sbrpt.AppendLine("more than one entry in flow for comid= " + comid);
                    else if (rowsElev.Length < 0)
                        sbrpt.AppendLine("no matching entry in flow for comid= " + comid);
                }

                //should only be 1
                DataRow[] rowsDA = dtDA.Select(comid);
                if (rowsDA.Length != 1)
                {
                    mergeErrCount++;
                    if (rowsDA.Length > 1)
                        sbrpt.AppendLine("more than one entry in DA for comid= " + comid);
                    else if (rowsElev.Length < 0)
                        sbrpt.AppendLine("no matching entry in DA for comid= " + comid);
                }

                //string comid1 = rowsPrecip[0].ItemArray[0].ToString();
                //string precipvc = rowsPrecip[0].ItemArray[1].ToString();
                //string maxelevsmo = rowsElev[0].ItemArray[1].ToString();
                //string minelevsmo = rowsElev[0].ItemArray[2].ToString();
                //string slope = rowsElev[0].ItemArray[3].ToString();
                //string maflowv = rowsFlow[0].ItemArray[1].ToString();
                //string mavelv = rowsFlow[0].ItemArray[2].ToString();
                //string totdasqkm = rowsDA[0].ItemArray[1].ToString();
                //string[] items2add = new string[] {comid1, totdasqkm, maxelevsmo, minelevsmo, slope, precipvc, maflowv, mavelv };

                string[] items2add = new string[] { rowsPrecip[0].ItemArray[0].ToString(), 
                                                    rowsDA[0].ItemArray[1].ToString(), 
                                                    rowsElev[0].ItemArray[1].ToString(), 
                                                    rowsElev[0].ItemArray[2].ToString(), 
                                                    rowsElev[0].ItemArray[3].ToString(), 
                                                    rowsPrecip[0].ItemArray[1].ToString(), 
                                                    rowsFlow[0].ItemArray[1].ToString(), 
                                                    rowsFlow[0].ItemArray[2].ToString() };
                DataRow dr2add = dt.NewRow();
                dr2add.ItemArray = items2add;
                dt.Rows.Add(dr2add);
                recsprocessed = r + 1;
            }

            sbrpt.AppendLine("merge error count: " + mergeErrCount.ToString());
            sbrpt.AppendLine("records processed: " + recsprocessed.ToString());
            return dt;
        }

        private DataTable GetDataTableFromCsv(string path, bool isFirstRowHeader)
        {
            string header = isFirstRowHeader ? "Yes" : "No";

            string pathOnly = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);

            string sql = @"SELECT * FROM [" + fileName + "]";

            using (OleDbConnection connection = new OleDbConnection(
                      @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + pathOnly +
                      ";Extended Properties=\"Text;HDR=" + header + "\""))
            using (OleDbCommand command = new OleDbCommand(sql, connection))
            using (OleDbDataAdapter adapter = new OleDbDataAdapter(command))
            {
                DataTable dataTable = new DataTable();
                dataTable.Locale = CultureInfo.CurrentCulture;
                adapter.Fill(dataTable);

                string sortcolName = dataTable.Columns[0].ToString();
                string sortParam = sortcolName + " asc";

                DataView dv = dataTable.DefaultView;
                dv.Sort = sortParam;
                DataTable dt = dv.ToTable();
                return dt;
            }
        }

        private DataTable createMasterDT()
        {
            //[COMID] INT KEY NOT NULL,
            //[TotDASqKM] REAL NULL,
            //[MAXELEVSMO] REAL NULL,
            //[MINELEVSMO] REAL NULL,
            //[SLOPE] REAL NULL,
            //[PrecipVC] REAL NULL,
            //[MAFLOWV] REAL NULL,
            //[MAVELV] REAL NULL

            DataTable dt = new DataTable();
            dt.Columns.Add("COMID", typeof(Int32));
            dt.Columns.Add("ToTDASqKm", typeof(double));
            dt.Columns.Add("MAXELEVSMO", typeof(double));
            dt.Columns.Add("MINELEVSMO", typeof(double));
            dt.Columns.Add("SLOPE", typeof(double));
            dt.Columns.Add("PrecipVC", typeof(double));
            dt.Columns.Add("MAFLOWV", typeof(double));
            dt.Columns.Add("MAVELV", typeof(double));

            return dt;
        }
    }
}
