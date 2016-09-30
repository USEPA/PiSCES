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

namespace CropChemApp
{
    public partial class Form2 : Form
    {
        //private string dbFile = "PISCESAttributes.db";
        private string dbFile = "NHDPlusv2Attributes.db";

        //utility reads user-selected csv/dbf nhdplusv2 attribute files (currently precip, elevslope, DA, sfVelocity) and
        //  1 - sequentially creates and/or appends pertinent data to csv files on disk 
        //  2 - as an intermediate step, sequentially reads those csv files back into memory prior to
        //  3 - sequentially creating sqlite data tables in the piscesattribute db
        public Form2()
        {
            InitializeComponent();
            CreatePiSCESDB();
        }

        private void CreatePiSCESDB()
        {
            //create the db
            //string dbFile = "PISCESAttributes.s3db";
            if (File.Exists(dbFile))
                File.Delete(dbFile);

            if (File.Exists(dbFile))
                SQLiteConnection.CreateFile(dbFile);
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
            writeSQLTable(sheet1FN);
            label1.Text = "Done with " + fileName;
        }

        private string BrwsCSVFile(string title)
        {
            //find the excel data sheets that are the source files to be merged
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

        Region csv_file_processing;
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

            StreamWriter sw = new StreamWriter("precip.csv");
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
                sql = "select COMID, MAFLOWV from " + Path.GetFileNameWithoutExtension(sheet1FN).ToString();
            }

            return sql;
        }

        Region sqlite_table_processing;
        private void writeSQLTable(string csvFileName)
        {
            SQLiteConnection dbConnection = new SQLiteConnection(string.Format("Data Source={0};Version=3;", dbFile));
            dbConnection.Open();
            IDbTransaction transaction1 = dbConnection.BeginTransaction();
            string tableName = string.Empty;
            //create the table we want
            string sql = GetCreateSqlTable(csvFileName, out tableName);
            SQLiteCommand command = new SQLiteCommand(sql, dbConnection);
            command.ExecuteNonQuery();
            transaction1.Commit();

            //get the data we want to write into the db table
            DataTable dt = ReadAttrCSVFile(csvFileName);
            //insert records into table
            transaction1 = dbConnection.BeginTransaction();
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                StringBuilder sbColNames = new StringBuilder("insert into " + tableName + " (");
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

                sql = sbColNames.ToString() + sbValues.ToString();

                command = new SQLiteCommand(sql, dbConnection);
                command.ExecuteNonQuery();
            }

            transaction1.Commit();
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
                sql = @"CREATE TABLE [SFFlowVelocity] (
                        [COMID] INT KEY NOT NULL,
                        [MAFLOWV] REAL NULL
                        );";
                tableName = "SFFlowVelocity";
            }
            return sql;

        }

        private void button3_Click(object sender, EventArgs e)
        {
            Close();
        }


    }
}
