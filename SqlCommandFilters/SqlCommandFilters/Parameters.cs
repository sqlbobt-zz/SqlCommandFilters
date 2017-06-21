
//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

//
//  NOTE: In order to support new language elements, the TSql130Parser and Sql130ScriptGenerator must be the current versions
// 

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;


namespace SqlCommandFilters
{
    using Microsoft.SqlServer.TransactSql.ScriptDom;
    /// <summary>
    /// Parameters is the public interface to the tool
    /// It contains a single static method to parameterize the SqlCommand.CommandText
    /// </summary>
    public class Parameters 
    {
        /// <summary>
        /// Parameterize is the static entry point into the tool
        /// It will take an existing SqlCommand object's CommandText
        /// Parse it, parameterize, add a Parameters collection to the SqlCommand Object
        /// and reparse to validate the changes
        /// </summary>
        /// <param name="cmd">a reference to thw SqlCommand object to be parameterized</param>
        /// <param name="reparse">Whether or not to reparse after completion. Default value is true</param>  
        /// <example> 
        /// <code>
        ///   using SqlCommandFilters;
        ///   class MyClass 
        ///   {
        ///      public static int Main() 
        ///      {
        ///         SqlConnectionStringBuilder sb = new SqlConnectionStringBuilder();
        ///         sb.InitialCatalog = "AdventureWorks";
        ///         sb.IntegratedSecurity = true;
        ///         sb.DataSource = "SQLBOBT";
        ///         SqlConnection con = new SqlConnection(sb.ConnectionString);
        ///         con.Open();
        ///         SqlCommand cmd = new SqlCommand();
        ///         cmd.Connection = con;
        ///         // Pick one of the TestStatements and assign it to the CommandText property of the SqlCommand object
        ///         cmd.CommandText = TestStatements.inSubQueryStmt;
        ///         SqlCommandFilters.Parameters.Parameterize(ref cmd);         
        ///         //NOTE: the default for reparse is true and is not required 
        ///         //If you don't want to reparse for performance reasons
        ///         //call it this way instead
        ///         SqlCommandFilters.Parameters.Parameterize(ref cmd, false);
        ///      }
        ///   }
        /// </code>
        /// </example>
        public static void Parameterize(ref SqlCommand cmd, bool reparse = true)
        {
            // Capture the current CommandText in a format that the parser can work with
            TextReader rdr = new StringReader(cmd.CommandText);
            
            // Setup to parse the T-SQL
            IList<ParseError> errors = null;

            //Using SQL 2016 parser
            TSql130Parser parser = new TSql130Parser(true);

            // Using SQL 2014 parser
            //TSql120Parser parser = new TSql120Parser(true);

            // Using SQL 2012 parser
            //TSql110Parser parser = new TSql110Parser(true);

            // Get the parse tree
            TSqlFragment tree = parser.Parse(rdr, out errors);
            // clean up resources
            rdr.Dispose();
            // if we could not parse the SQL we will throw an exception.Better here then on the server
            if (errors.Count > 0)
            {
                Exception e = new Exception(string.Format("Parse Error after converstion on line {0}, column {1}: {2}", errors[0].Line, errors[0].Column, errors[0].Message));
                throw e;
            }
            else
            {
                // Use the vistor pattern to examine the parse tree
                TsqlBatchVisitor visit = new TsqlBatchVisitor(cmd, reparse);
                // Now walk the tree
                tree.Accept(visit);
            }

         }
    }
    /// <summary>
    /// TsqlBatchVisitor is the implmentation of the TsqlFragmentVisitor class
    /// Its purpose is to process the batches, parameterize literals
    /// and construct the appropriate SqlCommand.Paramaters collection
    /// </summary>
    public class TsqlBatchVisitor : TSqlFragmentVisitor
    {
        #region Properties

        private SqlCommand cmd;
        /// <summary>
        /// SqlCommand - this parameter is used to support the processing of the CommandText property
        /// </summary>
        public SqlCommand Cmd
        {
            get { return cmd; }
            set { cmd = value; }
        }
        #endregion Properties

        #region internal
        // StringBuilder used to build the new SQL statement
        private StringBuilder SqlStmt = new StringBuilder("");
        // Used to build our paramater names
        private int parameterNumber = 0;
        // Used to prevent/allow reparse and formatting of produced code
        private bool reparse = true;
        #endregion internal

        /// <summary>
        /// Constructor for our WhereVisitor class
        /// </summary>
        /// <param name="cmd">A copy of the SqlCommand object</param>
        /// <param name="reparse">a flag to indicate whether or not we want to reparse after parameterization</param>
        #region Constructor
        public TsqlBatchVisitor(SqlCommand cmd, bool reparse)
        {
            this.Cmd = cmd;
            this.reparse = reparse;
            // Allow for partially parameterized querys. If there is already parameters in the collection, don't over-write them!
            parameterNumber = this.cmd.Parameters.Count;
        }
        #endregion Constructor

        #region Methods
        /// <summary>
        /// This is the entry point into the Visitor pattern and where all the work occurs
        /// Note we are explicitly triggering on SqlStmt node type
        /// This is because we only need to parameterize the search conditions
        /// </summary>
        /// <param name="node">This is the TsqlFrament sent to us from the Vistor.Accept method</param>
        public override void ExplicitVisit(TSqlBatch node)
        {

            // First determine which tokens belongs to our where clause
            int index = node.FirstTokenIndex;
            int end  = node.LastTokenIndex;

            
            // Now process each token in an appropriate manner
            while (index <= end)
            {
                // Use the TokenType to decide what processing needs to occur
                TSqlParserToken token = node.ScriptTokenStream[index];

                // Emit the token, and if necessary add a parameter to the parameters collection
                EmitToken(node, token, index);
                
                // Until we have processed all the tokens associated with the where clause
                index++;
            }
            // Now we must emit the rest of the tokens to get our entire T-SQL script

            if (reparse)
            {
                // Just a bonus - part of the ScriptDom namespace; side effect it revalidates our new code
                FormatSQL();
            }
            // let the base class finish up
            base.ExplicitVisit(node);
        }
        /// <summary>
        /// This is the logic for how to handle the tokens - i.e. copy to output stream, add to parameter collection etc.
        /// </summary>
        /// <param name="node">This is the TsqlFrament sent to the ExplicitVisit method</param>
        /// <param name="token">This is the current token that we are processing</param>
        /// <param name="index">The index to process</param>
        //protected void EmitToken(WhereClause node, TSqlParserToken token, int index)
        protected void EmitToken(TSqlBatch node, TSqlParserToken token, int index)
        {
            switch (token.TokenType)
            {
                // for the majority of TokenTypes we just pass the token to our StringBuilder for inclusion
                default:
                    SqlStmt.Append(node.ScriptTokenStream[index].Text);
                    break;

                // for those token types that may need to be parameterized we capture name, data type and value for the parameters collection
                case TSqlTokenType.AsciiStringLiteral:
                case TSqlTokenType.Real:
                case TSqlTokenType.Integer:
                case TSqlTokenType.Money:
                case TSqlTokenType.Numeric:
                case TSqlTokenType.UnicodeStringLiteral:
                    // We just use a simple naming scheme - i.e. @p1, @p2 and so on
                    string p = "@p" + (++parameterNumber).ToString();
                    SqlStmt.Append(p);
                    // Now create the entry in the SqlCommand Parameters collection
                    AddToParameterCollection(token, parameterNumber);
                    break;
            }
        }

        /// <summary>
        /// Uses the SQL Formatter to pretty print the code; not strictly necessary as the only place you will see it is in Profiler :)
        /// However, by reparsing the code we ensure that any errors in conversion are caught.
        /// </summary>
        /// <exception cref="Exception">Throws a generic exception if there is a parse error</exception>
        protected void FormatSQL()
        {
            if (reparse)
            {
                // use the features in the ScriptDom namespace to pretty print our T-SQL
                TextReader rdr = new StringReader(SqlStmt.ToString());
                IList<ParseError> errors = null;
                TSql130Parser parser = new TSql130Parser(true);
                TSqlFragment tree = parser.Parse(rdr, out errors);
                rdr.Close();
                if (errors.Count > 0)
                {
                    Exception e = new Exception(string.Format("Parse Error after converstion on line {0}, column {1}: {2}", errors[0].Line, errors[0].Column, errors[0].Message));
                    throw e;
                }
                else
                {
                    Sql130ScriptGenerator scrGen = new Sql130ScriptGenerator();
                    string formattedSQL = null;
                    scrGen.GenerateScript(tree, out formattedSQL);
                    cmd.CommandText = formattedSQL;
                }
            }
            else
            {
                cmd.CommandText = SqlStmt.ToString();
            }


        }

        /// <summary>
        /// Used to add a new parameter to the SqlCommand parameters collection
        /// </summary>
        /// <param name="token">The TsqlParserToken to be processed</param>
        /// <param name="parameterNumber"> The next value for our parameter name</param>
        protected void AddToParameterCollection(TSqlParserToken token, int parameterNumber)
        {
            // we will use a simple incrementing @p sequence
            string parm = "@p" + parameterNumber.ToString();
            
            // Default to string if none of the special types
            DbType parmType = DbType.String;
            // figure out the translation from token to DbType for use in adding to the Parameters collection below
            switch (token.TokenType)
            {
                case TSqlTokenType.AsciiStringLiteral:
                case TSqlTokenType.AsciiStringOrQuotedIdentifier:
                case TSqlTokenType.HexLiteral:
                    parmType = DbType.AnsiString;
                    break;
                case TSqlTokenType.UnicodeStringLiteral:
                    parmType = DbType.String;
                    break;
                case TSqlTokenType.Integer:
                    parmType = DbType.Int64;
                    break;
                case TSqlTokenType.Real:
                    parmType = DbType.Double;
                    break;
                case TSqlTokenType.Numeric:
                    parmType = DbType.Decimal;
                    break;
                case TSqlTokenType.Money:
                    parmType = DbType.Currency;
                    break;

            }

            // Add to the SqlCommand.Parameters collection using the meta-data collected above
            SqlParameter p = new SqlParameter();
            p.ParameterName = parm;
            p.DbType = parmType;
            switch(p.DbType)
            { 
                case DbType.AnsiString:
                    p.Value = token.Text.Substring(1, token.Text.Length - 2).Replace("''","'");
                    break;
                case DbType.String:
                    p.Value = token.Text.Substring(2, token.Text.Length - 3).Replace("''", "'");
                    break;
                default:
                    p.Value = token.Text;
                    break;
            }
            p.Direction = ParameterDirection.Input;
            cmd.Parameters.Add(p);

        }
        #endregion Methods
    }
}
