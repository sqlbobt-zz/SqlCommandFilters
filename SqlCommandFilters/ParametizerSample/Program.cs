//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************
using System;
using System.Data.SqlClient;


namespace ParametizerSample
{
    using SqlCommandFilters;
    class Program
    {
        static void Main(string[] args)
        {
            // Let's build a connection string (this would normally be done already)
            SqlConnectionStringBuilder sb = new SqlConnectionStringBuilder();
            sb.InitialCatalog = "AdventureWorks";
            sb.IntegratedSecurity = true;
            sb.DataSource = @"SQLBOBT2";
            SqlConnection con = new SqlConnection(sb.ConnectionString);

            //Set up the console window - note font was configured prior to this in the console windows properties dialog
            Console.Title = "SqlCommandFilters Demo";
            
            // Housekeeping variables            
            int input = 1;
            int max = TestStatements.statements.GetUpperBound(0);
            string selectionMessage = string.Format("Enter a value between 1-{0} to use a pre-defined query. Enter a negative number to quit", max);
            
            //Let's get one of the pre-defined queries
            do
            {
                WriteLineInColor(selectionMessage, ConsoleColor.Yellow);
                input = Convert.ToInt32(Console.ReadLine());
                Console.Clear();
                if (input >= 1 && input <= TestStatements.statements.GetUpperBound(0))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = con;

                    // Pick one of the TestStatements and assign it to the CommandText property of the SqlCommand object
                    cmd.CommandText = TestStatements.statements[input];

                    WriteLineInColor("Before parameterization", ConsoleColor.Yellow);
                    Console.WriteLine(); 
                    WriteLineInColor(cmd.CommandText, ConsoleColor.White);
                    Console.WriteLine();

                    // Parameterize supports a reparse parameter
                    // by default it is true. It will reparse and format the resultant SQL to ensure we have good code
                    // if you feel that the performance suffers you can turn off by calling this instead
                    // Parameters.Parameterize(ref cmd, false);
                    // Parameterize will parse, parameterize and create the parameter collection and modify the CommandText and Parameters collection
                    // appropriately

                    SqlCommandFilters.Parameters.Parameterize(ref cmd);
                    
                    // If you want to see the output of the call you can enhance the following snippet
                    // Also allows you to view in a Profiler trace
                    // By executing we will throw sytax and parsing errors

                    SqlDataReader rdr = cmd.ExecuteReader();
                    //while (rdr.Read())
                    //{
                    //    string r = rdr[0].ToString();
                    //    Console.WriteLine(r);
                    //}
                    // Done with connection make sure we close it
                    con.Close();

                    // Now display our results
                    WriteLineInColor("After parameterization", ConsoleColor.Yellow);
                    Console.WriteLine();
                    WriteLineInColor("Parameter collection", ConsoleColor.Yellow);
                    int pCount = 1;
                    foreach(SqlParameter p in cmd.Parameters)
                    {
                        string parmInfo = CreateParameterString(pCount, p);
                        if (pCount++ < cmd.Parameters.Count)
                        {
                            WriteInColor(parmInfo+", ", ConsoleColor.White, false);
                        }
                        else
                        {
                            WriteInColor(parmInfo , ConsoleColor.White, true);
                        }
                        
                    }

                    Console.WriteLine(cmd.CommandText);
                }
                else
                {
                    if (input < 1 || input > TestStatements.statements.GetUpperBound(0))
                    {
                        Console.WriteLine("Valid inputs are 1- {0} inclusive. Enter a negative number to end",max);
                    }
                }
            }
            while (input > 0);
        }
        static void WriteLineInColor(string msg, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(msg);
            Console.ForegroundColor = ConsoleColor.White;
        }
        static void WriteInColor(string msg, ConsoleColor color,bool done)
        {
            Console.ForegroundColor = color;
            Console.Write(msg);
            if (done)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine();
                Console.WriteLine();
            }
        }
        static string CreateParameterString(int pCount, SqlParameter p)
        {
            string retValue = "";
            if (p.SqlDbType != System.Data.SqlDbType.VarChar && p.SqlDbType != System.Data.SqlDbType.NVarChar)
            {
                retValue = string.Format("@P{0}= {1}", pCount, p.Value.ToString());
            }
            else
            {
                if (p.SqlDbType == System.Data.SqlDbType.NVarChar)
                {
                    retValue = string.Format("@P{0}= N'{1}'", pCount, p.Value.ToString());
                }
                else
                {
                    retValue = string.Format("@P{0}= '{1}'", pCount, p.Value.ToString());
                }
                
            }
            return retValue;
        }
    }
}
