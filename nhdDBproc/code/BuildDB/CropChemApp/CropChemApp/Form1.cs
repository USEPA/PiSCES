using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Data.SQLite;

namespace CropChemApp
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

        }

        private void button1_Click(object sender, EventArgs e)
        {
            string sheet1FN = BrwsCSVFile("Find exported sheet1");
            string sheet2FN = BrwsCSVFile("Find exported sheet2");
            string file1 = ReadCSVFile(sheet1FN);
            string file2 = ReadCSVFile(sheet2FN);
            writeTextDBfile(file1, file2);
            //at this point you can either import into sqlite the file written in the last method call
            //or uncomment the next call that creates the db.  The method call is much faster than importing...
            //but the difference is double quotes around all fields containing commas in the db via the call.
            //Havne't figured that one out yet.  Maybe get rid of them in the datatable when read backin from disk...
            CreateDB();

            MessageBox.Show("DB created. DB csv file created too.", "DB ready", MessageBoxButtons.OK);

            Close();

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

        private string ReadCSVFile (string filename)
        {
            //load the source file into memory
            string file = string.Empty;
            StreamReader sr = new StreamReader(filename);
            file = sr.ReadToEnd();
            sr.Close();
            return file;

        }

        private void writeTextDBfile(string dt1, string dt2)
        {
            //write a csv file suitable for import into sqlite
            string outfile = string.Empty;

            //break files into lines
            string [] file1 = dt1.Split('\n');
            string [] file2 = dt2.Split('\n');

            //create header line
            string [] h1 = file1[0].Split('\t');
            string [] h2 = file2[0].Split('\t');
            string header = makeString(h1,h2);
            outfile = outfile + header;

            //few assumptions here: input files (excel sheets to merge) have been exported to tab delimited files,
            //commas won't do because fields contain commas. 
            for (int line = 1; line < file1.Length - 1; line++)
            {
                string [] cols = file1[line].Split('\t');
                string key = cols[5].Trim();
                for (int line2 = 1; line2 < file2.Length - 1; line2++)
                {
                    string[] cols2 = file2[line2].Split('\t');
                    string key2 = cols2[4].Trim();
                    if (key == key2)
                    {
                        outfile = outfile + makeString(cols, cols2);
                    }
                }
            }

            //dump data to file - sqlite admin expects cvs file for data import
            StreamWriter sw = new StreamWriter("db.csv");
            sw.Write(outfile);
            sw.Close();
        }

        private string makeString(string[] a1, string[] a2)
        {
            //fields delimited by ";" (semicolons; for sqlite import)
            string retstring = string.Empty;
            foreach (string t1 in a1)
            {
                //string t = t1.Trim();
                string t = t1.Replace(";", ",").Trim();
                t = t1.Replace('"', ' ').Trim();
                retstring += t + ";";
            }
            foreach (string t2 in a2)
            {
                //string t = t2.Trim();
                string t = t2.Replace(";", ",").Trim();
                t = t2.Replace('"', ' ').Trim();
                retstring += t + ";";
            }
            retstring = retstring.Remove(retstring.Length - 1, 1);
            retstring = retstring + "\n";

            return retstring;
        }

        private void CreateDB()
        {
            //create and write the sqlite db file from the csv file just written.
            //read it into a datatable
            DataTable dt = ReadDBCSVFile("db.csv");

            string dbFile = "CropChemApplicationV2.s3db";
            if (File.Exists(dbFile))
                File.Delete(dbFile);

            //DirectoryInfo di = Directory.GetParent(dbFile);
            //if (!Directory.Exists(di.FullName))
            //    Directory.CreateDirectory(di.FullName);

            SQLiteConnection.CreateFile(dbFile);

            SQLiteConnection dbConnection = new SQLiteConnection(string.Format("Data Source={0};Version=3;", dbFile));
            dbConnection.Open();

            //Create sqlite table
            IDbTransaction transaction1 = dbConnection.BeginTransaction();
            string sql = GetCreateTableSQL();
            SQLiteCommand command = new SQLiteCommand(sql, dbConnection);
            command.ExecuteNonQuery();
            transaction1.Commit();


            //Insert records into table
            transaction1 = dbConnection.BeginTransaction();
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                StringBuilder sbColNames = new StringBuilder("insert into CCA (");
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

            MessageBox.Show("Done", "AttaBoy", MessageBoxButtons.OK);

        }

        private DataTable ReadDBCSVFile(string file)
        {
            if (File.Exists(file) == false)
                throw new Exception("Could not find file: " + file);

            Dictionary<string, List<string>> dctFishHUC = new Dictionary<string, List<string>>();
            StreamReader sr = new StreamReader(file);
            //First line is headers
            string line = sr.ReadLine();
            string[] headers = line.Split(";".ToCharArray());

            DataTable dt = new DataTable();
            for (int i = 0; i < headers.Length; i++)
                dt.Columns.Add(headers[i]);


            while ((line = sr.ReadLine()) != null)
            {
                DataRow dr = dt.NewRow();
                string[] row = line.Split(";".ToCharArray());
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

        private string GetCreateTableSQL()
        {

            string sql = @"CREATE TABLE [CCA] (
                            [Crop] VARCHAR(60)  NULL,
                            [GrpNo] VARCHAR(20)  NULL,
                            [GrpName] VARCHAR(60)  NULL,
                            [SubGrpNo] VARCHAR(12)  NULL,
                            [SubGrpName] VARCHAR(60)  NULL,
                            [Category] VARCHAR(60)  NULL,
                            [Activity] VARCHAR(12)  NULL,
                            [Formulation] VARCHAR(40)  NULL,
                            [AppEquip] VARCHAR(40)  NULL,
                            [AppType] VARCHAR(40)  NULL,
                            [CategoryM] VARCHAR(60)  NULL,
                            [AppRateVal] VARCHAR(8) NULL,
                            [AppRateUnit] VARCHAR(16) NULL,
                            [TreatedVal] VARCHAR(8) NULL,
                            [TreatedUnit] VARCHAR(16) NULL,
                            [DUESLNoG] VARCHAR(24) NULL,
                            [DUESLG] VARCHAR(24) NULL,
                            [DUEDLG] VARCHAR(24) NULL,
                            [DUESLGCRH] VARCHAR(24) NULL,
                            [DUEDLGCRH] VARCHAR(24) NULL,
                            [DUEEC] VARCHAR(24) NULL,
                            [IUENoR] VARCHAR(24) NULL,
                            [IUEPF5R] VARCHAR(24) NULL,
                            [IUEPF10R] VARCHAR(24) NULL,
                            [IUEEC] VARCHAR(24) NULL,
                            [DSCategory] VARCHAR(40) NULL,
                            [DSMRID] VARCHAR(40) NULL,
                            [DSDesc] VARCHAR(60) NULL,
                            [DSDER] VARCHAR(40) NULL
                            );";

            return sql;
        }

        //pisces db processing
        private void button2_Click(object sender, EventArgs e)
        {
            Form2 frm2 = new Form2();
            frm2.ShowDialog();
            
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Close();
        }

    }
}
