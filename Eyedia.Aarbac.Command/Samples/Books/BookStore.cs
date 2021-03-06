#region Copyright Notice
/* Copyright (c) 2017, Deb'jyoti Das - debjyoti@debjyoti.com
 All rights reserved.

 Redistribution and use in source and binary forms, with or without
 modification, are not permitted.Neither the name of the 
 'Deb'jyoti Das' nor the names of its contributors may be used 
 to endorse or promote products derived from this software without 
 specific prior written permission.

 THIS SOFTWARE IS PROVIDED BY Deb'jyoti Das 'AS IS' AND ANY
 EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 DISCLAIMED. IN NO EVENT SHALL Synechron Holdings Inc BE LIABLE FOR ANY
 DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
#region Developer Information
/*
Author  - Debjyoti Das (debjyoti@debjyoti.com)
Created - 11/12/2017 3:31:31 PM
Description  - 
Modified By - 
Description  - 
*/
#endregion Developer Information

#endregion Copyright Notice
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eyedia.Aarbac.Framework;
using System.Data;
using System.IO;
using GenericParsing;
using System.Diagnostics;
using System.Data.SqlClient;

namespace Eyedia.Aarbac.Command
{
    public class BookStore
    {           
        public BookStore()
        {
            string s = _rootDir;
        }

        private string _rootDir
        {
            get
            {
                string path = string.Empty;
                string codingdir = Path.GetDirectoryName(Path.GetDirectoryName(Directory.GetCurrentDirectory()));
                if (Directory.Exists(Path.Combine(codingdir, "Samples")))
                    path = Path.Combine(codingdir, "Samples");
                else if (Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Samples")))
                    path = Path.Combine(codingdir, "Samples");
                else
                    throw new DirectoryNotFoundException("Samples directory not found!");

                var dbPath = Path.Combine(path, "Databases");                
                AppDomain.CurrentDomain.SetData("DataDirectory", Path.GetFullPath(dbPath));
                return path;
            }
        }

        private static readonly Random random = new Random(0);
        private static readonly object syncLock = new object();
        public static int RandomNumber(int min, int max)
        {
            lock (syncLock)
            { // synchronize
                return random.Next(min, max) * (max - min) + min;
               
            }
        }
  
        public void Setup()
        {
            Rbac rbac = new Rbac();
            rbac = rbac.CreateNew("books", "books description",
                @"Data Source=LPT-03084856325\SQLEXPRESS;Initial Catalog=books;Integrated Security=True",
                File.ReadAllText(Path.Combine(_rootDir,"Books","entitlement.xml")));
                       
            InsertRoles(rbac);
            GenericParserAdapter parser = new GenericParserAdapter(Path.Combine(_rootDir, "Books", "BooksUsers.csv"));
            parser.FirstRowHasHeader = true;
            DataTable table = parser.GetDataTable();

            
            if (table.Rows.Count > 0)
            {                
                foreach (DataRow dataRow in table.Rows)
                {
                    RbacRole role = roles.Where(r => r.Name == dataRow["Role"].ToString()).SingleOrDefault();
                    if (role == null)
                        throw new Exception(dataRow["Role"].ToString() + " is not defined!");

                    RbacUser user = Rbac.CreateUser(dataRow[0].ToString(), dataRow[1].ToString(), dataRow[2].ToString(), "password", role);
                    if (role.Name == "role_city_mgr")
                    {
                        user.AddParameter("{CityNames}", "('New York','Charlotte')");                        
                    }
                    else if (role.Name == "role_state_mgr")
                    {
                        user.AddParameter("{ShortNames}", "('NY','NC')");
                    }
                    else if (role.Name == "role_country_mgr")
                    {
                        user.AddParameter("{CountryCodes}", "('IN','US')");
                    }
                }
            }
        }
        List<RbacRole> roles = new List<RbacRole>();
        private void InsertRoles(Rbac rbac)
        {
            string path = Path.Combine(_rootDir, "Books");
            string entitlements = File.ReadAllText(Path.Combine(_rootDir, "Books", "entitlement.xml"));

            string[] roleFiles = Directory.GetFiles(path, "role_*.xml");
            foreach (string roleFile in roleFiles)
            {
                string strRle = File.ReadAllText(roleFile);
                string strDescription = File.ReadAllText(Path.Combine(Path.GetDirectoryName(roleFile),
                    Path.GetFileNameWithoutExtension(roleFile) + ".txt"));

                RbacRole role = rbac.CreateRole(Path.GetFileNameWithoutExtension(roleFile)
                    , strDescription, strRle, entitlements);
                roles.Add(role);

            }
        }
        public RbacSqlQueryEngine TestOne(string query = null)
        {
            File.WriteAllText(Path.Combine(_rootDir, "Books", "test_parsed_query.txt"), string.Empty);

            RbacSqlQueryEngine engine = null;
            using (Rbac rbac = new Rbac("Lashawn"))
            {
                if (string.IsNullOrEmpty(query))
                    query = File.ReadAllText(Path.Combine(_rootDir, "Books", "test.txt"));
                engine = new RbacSqlQueryEngine(rbac, query);
                engine.Execute();
                //if ((!engine.IsErrored) && (engine.SqlQueryParser.IsParsed) && (engine.SqlQueryParser.QueryType == RbacQueryTypes.Select))
                //    table = engine.Table; //--> gives you data table if it is a select query

            }
            if (!string.IsNullOrEmpty(engine.AllErrors))
                Console.WriteLine("Errors:{0}", engine.AllErrors);

            if ((engine.Parser.QueryType == RbacQueryTypes.Select) && (engine.Table != null))
                Console.WriteLine("The query was a select query and returned {0} records", engine.Table.Rows.Count);

            File.WriteAllText(Path.Combine(_rootDir, "Books", "test_parsed_query.txt"), engine.Parser.ParsedQuery);
            return engine;
        }
        public void TestBatch()
        {
            
            GenericParserAdapter genParser = new GenericParserAdapter(Path.Combine(_rootDir, "Books", "tests.csv"));
            genParser.FirstRowHasHeader = true;
            DataTable table = genParser.GetDataTable();
            if (table.Columns["ParsedQueryStage1"] == null)
            {
                table.Columns.Add("ParsedQueryStage1");
                table.Columns.Add("ParsedQuery");
                table.Columns.Add("Records");
                table.Columns.Add("Errors");
                table.Columns.Add("TestResult");
            }
            bool cleaned = false;
            foreach (DataRow row in table.Rows)
            {
                //if (row["Id"].ToString() == "11")
                //    Debugger.Break();

                Rbac rbac = new Rbac(row["User"].ToString());
                RbacRole role = Rbac.GetRole(row["Role"].ToString());

                if(!cleaned)
                {
                    CleanDataFromDb(rbac.ConnectionString);
                    cleaned = true;
                }
                SqlQueryParser parser = new SqlQueryParser(rbac);
                try
                {
                    parser.Parse(row["Query"].ToString());
                }
                catch (Exception ex)
                {
                    row["Errors"] = ex.Message;
                    if (row["Expected"].ToString().Equals(row["Errors"].ToString()))
                        row["TestResult"] = "Passed";
                    else
                        row["TestResult"] = "Failed";
                    continue;
                }
                row["ParsedQueryStage1"] = parser.ParsedQueryStage1;
                row["ParsedQuery"] = parser.ParsedQuery;
                row["Errors"] += parser.AllErrors;

                if (string.IsNullOrEmpty(parser.AllErrors))
                {
                    RbacSqlQueryEngine engine = new RbacSqlQueryEngine(parser, true);
                    engine.Execute();                    
                    if (engine.IsErrored)
                        row["Records"] = "Errored";
                    else if ((parser.QueryType == RbacQueryTypes.Select) && (engine.Table == null))
                        row["Records"] = "Errored";
                    else if ((parser.QueryType == RbacQueryTypes.Select) && (engine.Table != null))
                        row["Records"] = engine.Table.Rows.Count + " record(s)";

                    if (!string.IsNullOrEmpty(parser.AllErrors))
                        row["Errors"] += parser.AllErrors + Environment.NewLine;

                    if (!string.IsNullOrEmpty(engine.AllErrors))
                        row["Errors"] += engine.AllErrors + Environment.NewLine;
                }

                if (row["Expected"].ToString().Equals(row["Errors"].ToString()))
                    row["TestResult"] = "Passed";
                else
                    row["TestResult"] = "Failed";

            }
            table.ToCsv(Path.Combine(_rootDir, "Books", "tests_result.csv"));
            ToCsvMarkdownFormat(table, Path.Combine(_rootDir, "Books", "tests_result.md"));
        }

        private void CleanDataFromDb(string connectionString)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand("drop table Author2", connection);
                    command.ExecuteNonQuery();
                    connection.Close();
                }
            }
            catch
            {
               
            }
        }

        public void ToCsvMarkdownFormat(DataTable table, string fileName)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Resources.MD_Sample_Query_TopText);
            string rbac = "books";

            int counter = 1;
            foreach (DataRow row in table.Rows)
            {
                string oneRecord = string.Format("{0}. **{1}**  {2}", counter, row["Comment"], Environment.NewLine);                
                oneRecord += String.Format("Rbac: {0}  {1}", rbac, Environment.NewLine);
                oneRecord += String.Format("User: {0}  {1}", row["User"], Environment.NewLine);
                oneRecord += String.Format("Role: [{0}](https://raw.githubusercontent.com/eyedia/aarbac/master/Eyedia.Aarbac.Command/Samples/Books/{0}.xml)  {1}",
                    row["Role"], Environment.NewLine);
                oneRecord += String.Format("Query:  {0}", Environment.NewLine);               


                oneRecord += "```sql" + Environment.NewLine + FormatQuery(row["Query"]) + Environment.NewLine + "```" + Environment.NewLine;

                if (!string.IsNullOrEmpty(row["ParsedQuery"].ToString()))
                {
                    oneRecord += "```" + Environment.NewLine;
                    oneRecord += "Parsed Query:" + Environment.NewLine;
                    oneRecord += "```" + Environment.NewLine;
                    oneRecord += "```sql" + Environment.NewLine + FormatQuery(row["ParsedQuery"]) + Environment.NewLine + "```" + Environment.NewLine;
                }
                
                //if ((!string.IsNullOrEmpty(row["Recor'ds"].ToString()))
                //    && (row["Records"].ToString() != "Errored"))              
                //    oneRecord += "```" + Environment.NewLine + "Record Count(s):" + row["Records"] + Environment.NewLine + "```" + Environment.NewLine;

                if (!string.IsNullOrEmpty(row["Errors"].ToString()))           
                    oneRecord += "```diff" + Environment.NewLine + "- " + row["Errors"] + Environment.NewLine + "```" + Environment.NewLine;

                
                oneRecord += "***" + Environment.NewLine;

                sb.AppendLine(oneRecord);
                counter++;
            }

            File.WriteAllText(fileName, sb.ToString());
        }
        private string FormatQuery(object cell)
        {
            string fQuery = cell.ToString().Replace("where", Environment.NewLine + "where").Replace("inner", Environment.NewLine + "inner");
            fQuery = fQuery.Replace("WHERE", Environment.NewLine + "WHERE");
            fQuery = fQuery.Replace(" in ", Environment.NewLine + " in ");
            fQuery = fQuery.Replace(Environment.NewLine + Environment.NewLine, Environment.NewLine);
            return fQuery;
        }
    }
}

